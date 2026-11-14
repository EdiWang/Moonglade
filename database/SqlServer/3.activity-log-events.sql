-- SQL Server: replace legacy activity history with stable activity event keys.
-- Target: the release introducing stable activity event keys.
-- Prerequisite: category integer IDs; dbo.ActivityLog must have the complete
-- standard legacy or new shape. SQL Server retains PascalCase names.
-- Stop all application instances/background workers, back up, and rehearse on
-- a restored copy. Run this complete script standalone with maintenance rights.
-- Only the recognized legacy cutover deletes activity history. Other tables,
-- the existing table/primary key/identity generator, and new history are retained.

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET LOCK_TIMEOUT 10000;

IF @@TRANCOUNT <> 0
    THROW 51000, 'Run the activity upgrade standalone, outside an existing transaction.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Activity int = OBJECT_ID(N'dbo.ActivityLog', N'U');
    IF @Activity IS NULL
        THROW 51000, 'Activity upgrade requires dbo.ActivityLog. No history was deleted.', 1;
    IF COALESCE(HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'VIEW DEFINITION'), 0) <> 1
        THROW 51000, 'Activity upgrade requires database VIEW DEFINITION to inspect all dependencies. No history was deleted.', 1;

    DECLARE @RowCount bigint;
    EXEC sys.sp_executesql
        N'SELECT @RowCount = COUNT_BIG(*) FROM dbo.ActivityLog WITH (TABLOCKX, HOLDLOCK);',
        N'@RowCount bigint OUTPUT', @RowCount OUTPUT;
    IF OBJECT_ID(N'dbo.ActivityLog', N'U') <> @Activity
        THROW 51000, 'Activity table identity changed while acquiring the lock. No history was deleted.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.tables WHERE object_id = @Activity
            AND name COLLATE Latin1_General_100_BIN2 = N'ActivityLog'
            AND SCHEMA_NAME(schema_id) COLLATE Latin1_General_100_BIN2 = N'dbo'
            AND temporal_type = 0 AND is_memory_optimized = 0 AND is_filetable = 0
            AND is_tracked_by_cdc = 0 AND is_replicated = 0 AND is_merge_published = 0
            AND is_sync_tran_subscribed = 0)
        THROW 51000, 'Unexpected activity table naming, temporal, memory, CDC, or replication configuration. No history was deleted.', 1;

    DECLARE @Expected TABLE (
        Shape varchar(6) NOT NULL,
        ColumnName sysname NOT NULL,
        TypeId int NOT NULL,
        MaxLength smallint NOT NULL,
        Scale tinyint NOT NULL,
        IsNullable bit NOT NULL
    );
    -- sys.columns reports nvarchar limits in bytes, not characters.
    INSERT INTO @Expected (Shape, ColumnName, TypeId, MaxLength, Scale, IsNullable)
    VALUES
        ('legacy', N'Id', TYPE_ID(N'bigint'), 8, 0, 0),
        ('legacy', N'EventId', TYPE_ID(N'int'), 4, 0, 0),
        ('legacy', N'EventTimeUtc', TYPE_ID(N'datetime2'), 8, 7, 1),
        ('legacy', N'ActorId', TYPE_ID(N'nvarchar'), 200, 0, 1),
        ('legacy', N'Operation', TYPE_ID(N'nvarchar'), 200, 0, 1),
        ('legacy', N'TargetName', TYPE_ID(N'nvarchar'), 400, 0, 1),
        ('legacy', N'MetaData', TYPE_ID(N'nvarchar'), -1, 0, 1),
        ('legacy', N'IpAddress', TYPE_ID(N'nvarchar'), 100, 0, 1),
        ('legacy', N'UserAgent', TYPE_ID(N'nvarchar'), 1024, 0, 1),
        ('new', N'Id', TYPE_ID(N'bigint'), 8, 0, 0),
        ('new', N'EventKey', TYPE_ID(N'nvarchar'), 200, 0, 0),
        ('new', N'EventTimeUtc', TYPE_ID(N'datetime2'), 8, 7, 0),
        ('new', N'ActorKind', TYPE_ID(N'int'), 4, 0, 0),
        ('new', N'ActorDisplayName', TYPE_ID(N'nvarchar'), 200, 0, 1),
        ('new', N'TargetId', TYPE_ID(N'nvarchar'), 200, 0, 1),
        ('new', N'TargetDisplayName', TYPE_ID(N'nvarchar'), 400, 0, 1),
        ('new', N'IpAddress', TYPE_ID(N'nvarchar'), 100, 0, 1),
        ('new', N'UserAgent', TYPE_ID(N'nvarchar'), 1024, 0, 1),
        ('new', N'Details', TYPE_ID(N'nvarchar'), -1, 0, 1);

    DECLARE @ColumnCount int = (SELECT COUNT(*) FROM sys.columns WHERE object_id = @Activity);
    DECLARE @LegacyShape bit = 0, @NewShape bit = 0;
    IF @ColumnCount = 9 AND NOT EXISTS (
        SELECT 1 FROM @Expected e
        LEFT JOIN sys.columns c ON c.object_id = @Activity
            AND c.name COLLATE Latin1_General_100_BIN2 = e.ColumnName COLLATE Latin1_General_100_BIN2
        WHERE e.Shape = 'legacy' AND (c.column_id IS NULL OR c.system_type_id <> e.TypeId
            OR c.user_type_id <> e.TypeId OR c.max_length <> e.MaxLength
            OR c.scale <> e.Scale OR c.is_nullable <> e.IsNullable))
        SET @LegacyShape = 1;
    IF @ColumnCount = 10 AND NOT EXISTS (
        SELECT 1 FROM @Expected e
        LEFT JOIN sys.columns c ON c.object_id = @Activity
            AND c.name COLLATE Latin1_General_100_BIN2 = e.ColumnName COLLATE Latin1_General_100_BIN2
        WHERE e.Shape = 'new' AND (c.column_id IS NULL OR c.system_type_id <> e.TypeId
            OR c.user_type_id <> e.TypeId OR c.max_length <> e.MaxLength
            OR c.scale <> e.Scale OR c.is_nullable <> e.IsNullable))
        SET @NewShape = 1;
    IF @LegacyShape = 0 AND @NewShape = 0
        THROW 51000, 'Unrecognized activity column names, types, lengths, or nullability (expected all 9 legacy or all 10 new columns). No history was deleted.', 1;

    DECLARE @Id int = COLUMNPROPERTY(@Activity, N'Id', 'ColumnId');
    IF NOT EXISTS (
        SELECT 1 FROM sys.identity_columns
        WHERE object_id = @Activity AND column_id = @Id
            AND system_type_id = TYPE_ID(N'bigint') AND is_nullable = 0)
        OR EXISTS (
            SELECT 1 FROM sys.columns WHERE object_id = @Activity
                AND (is_computed = 1 OR default_object_id <> 0 OR rule_object_id <> 0
                    OR is_sparse = 1 OR is_column_set = 1 OR is_filestream = 1
                    OR is_rowguidcol = 1 OR generated_always_type <> 0 OR is_hidden = 1
                    OR encryption_type IS NOT NULL OR (column_id <> @Id AND is_identity = 1)))
        OR EXISTS (SELECT 1 FROM sys.masked_columns WHERE object_id = @Activity AND is_masked = 1)
        THROW 51000, 'Unexpected activity identity, generated column, default, rule, or special column configuration. No history was deleted.', 1;

    DECLARE @PrimaryKey int, @PrimaryKeyIndex int;
    SELECT @PrimaryKey = object_id, @PrimaryKeyIndex = unique_index_id
    FROM sys.key_constraints WHERE parent_object_id = @Activity AND type = 'PK';
    IF @PrimaryKey IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = @Activity AND index_id = @PrimaryKeyIndex
                AND type IN (1, 2) AND is_primary_key = 1 AND is_unique = 1
                AND is_disabled = 0 AND is_hypothetical = 0 AND has_filter = 0)
        OR (SELECT COUNT(*) FROM sys.index_columns
            WHERE object_id = @Activity AND index_id = @PrimaryKeyIndex) <> 1
        OR NOT EXISTS (
            SELECT 1 FROM sys.index_columns
            WHERE object_id = @Activity AND index_id = @PrimaryKeyIndex
                AND column_id = @Id AND key_ordinal = 1 AND is_included_column = 0)
        OR EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = @Activity AND index_id > 0 AND index_id <> @PrimaryKeyIndex)
        OR EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE parent_object_id = @Activity OR referenced_object_id = @Activity)
        OR EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = @Activity)
        OR EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = @Activity)
        THROW 51000, 'Unexpected activity primary key, foreign key, index, or constraint. No history was deleted.', 1;

    IF EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = @Activity)
        OR EXISTS (SELECT 1 FROM sys.triggers WHERE parent_class = 0 AND is_disabled = 0)
        OR EXISTS (SELECT 1 FROM sys.security_predicates WHERE target_object_id = @Activity)
        OR EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = @Activity)
        OR EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = @Activity)
        OR EXISTS (
            SELECT 1 FROM sys.stats
            WHERE object_id = @Activity AND user_created = 1 AND stats_id <> @PrimaryKeyIndex)
        OR EXISTS (
            SELECT 1 FROM sys.sql_expression_dependencies
            WHERE referencing_id = @Activity OR referenced_id = @Activity
                OR (referenced_entity_name COLLATE Latin1_General_100_BIN2 = N'ActivityLog'
                    AND referenced_schema_name COLLATE Latin1_General_100_BIN2 = N'dbo'
                    AND referenced_server_name IS NULL
                    AND (referenced_database_name IS NULL OR referenced_database_name = DB_NAME())))
        OR EXISTS (
            SELECT 1 FROM sys.synonyms
            WHERE PARSENAME(base_object_name, 1) COLLATE Latin1_General_100_BIN2 = N'ActivityLog'
                AND PARSENAME(base_object_name, 2) COLLATE Latin1_General_100_BIN2 = N'dbo'
                AND PARSENAME(base_object_name, 4) IS NULL
                AND (PARSENAME(base_object_name, 3) IS NULL OR PARSENAME(base_object_name, 3) = DB_NAME()))
        THROW 51000, 'Unexpected activity dependency, trigger, policy, tracking, synonym, manual statistics, or database DDL trigger. Have a DBA inspect the schema. No history was deleted.', 1;

    IF @NewShape = 1
    BEGIN
        -- Do not delete rows or reseed the identity, including after later writes.
        COMMIT TRANSACTION;
        RETURN;
    END;

    -- Keep shape-dependent DDL in a separate batch so it also compiles on reruns.
    EXEC sys.sp_executesql N'
        DELETE FROM dbo.ActivityLog;
        ALTER TABLE dbo.ActivityLog DROP COLUMN EventId, ActorId, Operation, TargetName, MetaData;
        ALTER TABLE dbo.ActivityLog ALTER COLUMN EventTimeUtc datetime2(7) NOT NULL;
        ALTER TABLE dbo.ActivityLog ADD
            EventKey nvarchar(100) NOT NULL,
            ActorKind int NOT NULL,
            ActorDisplayName nvarchar(100) NULL,
            TargetId nvarchar(100) NULL,
            TargetDisplayName nvarchar(200) NULL,
            Details nvarchar(max) NULL;';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
