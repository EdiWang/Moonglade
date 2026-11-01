-- SQL Server: upgrade the GUID-based Category schema to integer identity IDs.
-- Target: the release introducing category integer IDs (100, increment 1).
-- Prerequisites: dbo.Category and dbo.PostCategory with their original primary
-- keys, a Category foreign key, and a single-column CategoryId index.
-- Stop all application instances and back up the database before execution.
-- Run this standalone script before starting the upgraded application.
-- Category content and article GUIDs are preserved; old category GUIDs are not.

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Category int = OBJECT_ID(N'dbo.Category', N'U');
    DECLARE @PostCategory int = OBJECT_ID(N'dbo.PostCategory', N'U');
    IF @Category IS NULL OR @PostCategory IS NULL
        THROW 51000, 'Category migration requires dbo.Category and dbo.PostCategory.', 1;

    DECLARE @RowCount bigint;
    SELECT @RowCount = COUNT_BIG(*) FROM dbo.Category WITH (TABLOCKX, HOLDLOCK);
    SELECT @RowCount = COUNT_BIG(*) FROM dbo.PostCategory WITH (TABLOCKX, HOLDLOCK);

    DECLARE @Id int = COLUMNPROPERTY(@Category, N'Id', 'ColumnId');
    DECLARE @CategoryId int = COLUMNPROPERTY(@PostCategory, N'CategoryId', 'ColumnId');
    DECLARE @PostId int = COLUMNPROPERTY(@PostCategory, N'PostId', 'ColumnId');
    DECLARE @IdType sysname = (
        SELECT TYPE_NAME(system_type_id) FROM sys.columns WHERE object_id = @Category AND column_id = @Id);
    DECLARE @CategoryIdType sysname = (
        SELECT TYPE_NAME(system_type_id) FROM sys.columns WHERE object_id = @PostCategory AND column_id = @CategoryId);
    IF @PostId IS NULL OR NOT EXISTS (
        SELECT 1 FROM sys.columns WHERE object_id = @PostCategory AND column_id = @PostId
            AND system_type_id = TYPE_ID(N'uniqueidentifier'))
        THROW 51000, 'Category migration requires GUID article IDs in dbo.PostCategory.PostId.', 1;

    IF @IdType = N'int' AND @CategoryIdType = N'int'
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM sys.identity_columns
            WHERE object_id = @Category AND column_id = @Id
                AND CONVERT(bigint, seed_value) = 100 AND CONVERT(bigint, increment_value) = 1)
            OR EXISTS (
                SELECT 1 FROM sys.columns
                WHERE (object_id = @Category AND column_id = @Id
                    OR object_id = @PostCategory AND column_id = @CategoryId) AND is_nullable = 1)
            THROW 51000, 'Integer category columns exist but do not match the required identity schema.', 1;

        -- Do not reseed: subsequent inserts, deletions, and administrator edits
        -- must remain untouched when the script is run again.
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF @IdType IS NULL OR @CategoryIdType IS NULL OR @PostId IS NULL
        OR @IdType <> N'uniqueidentifier' OR @CategoryIdType <> N'uniqueidentifier'
        THROW 51000, 'Category migration requires GUID columns, or an already upgraded integer identity schema.', 1;
    IF COL_LENGTH(N'dbo.Category', N'IdInt') IS NOT NULL
        OR COL_LENGTH(N'dbo.PostCategory', N'CategoryIdInt') IS NOT NULL
        THROW 51000, 'Temporary migration columns already exist. Restore or inspect the schema before retrying.', 1;

    DECLARE @CategoryPk sysname, @CategoryPkIndex int, @CategoryPkType nvarchar(60);
    DECLARE @PostCategoryPk sysname, @PostCategoryPkIndex int, @PostCategoryPkType nvarchar(60);
    SELECT @CategoryPk = k.name, @CategoryPkIndex = k.unique_index_id, @CategoryPkType = i.type_desc
    FROM sys.key_constraints k
    JOIN sys.indexes i ON i.object_id = k.parent_object_id AND i.index_id = k.unique_index_id
    WHERE k.parent_object_id = @Category AND k.type = 'PK';
    SELECT @PostCategoryPk = k.name, @PostCategoryPkIndex = k.unique_index_id, @PostCategoryPkType = i.type_desc
    FROM sys.key_constraints k
    JOIN sys.indexes i ON i.object_id = k.parent_object_id AND i.index_id = k.unique_index_id
    WHERE k.parent_object_id = @PostCategory AND k.type = 'PK';

    IF @CategoryPk IS NULL OR @PostCategoryPk IS NULL
        OR (SELECT COUNT(*) FROM sys.index_columns
            WHERE object_id = @Category AND index_id = @CategoryPkIndex AND key_ordinal > 0) <> 1
        OR NOT EXISTS (SELECT 1 FROM sys.index_columns
            WHERE object_id = @Category AND index_id = @CategoryPkIndex AND column_id = @Id AND key_ordinal = 1)
        OR (SELECT COUNT(*) FROM sys.index_columns
            WHERE object_id = @PostCategory AND index_id = @PostCategoryPkIndex AND key_ordinal > 0) <> 2
        OR NOT EXISTS (SELECT 1 FROM sys.index_columns
            WHERE object_id = @PostCategory AND index_id = @PostCategoryPkIndex AND column_id = @PostId AND key_ordinal = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.index_columns
            WHERE object_id = @PostCategory AND index_id = @PostCategoryPkIndex AND column_id = @CategoryId AND key_ordinal = 2)
        THROW 51000, 'Unexpected category primary keys. Adapt the migration to this schema before execution.', 1;

    DECLARE @ForeignKey sysname, @DeleteAction nvarchar(60), @UpdateAction nvarchar(60);
    SELECT @ForeignKey = f.name,
        @DeleteAction = REPLACE(f.delete_referential_action_desc, '_', ' '),
        @UpdateAction = REPLACE(f.update_referential_action_desc, '_', ' ')
    FROM sys.foreign_keys f
    JOIN sys.foreign_key_columns c ON c.constraint_object_id = f.object_id
    WHERE f.parent_object_id = @PostCategory AND f.referenced_object_id = @Category
        AND c.parent_column_id = @CategoryId AND c.referenced_column_id = @Id
        AND (SELECT COUNT(*) FROM sys.foreign_key_columns WHERE constraint_object_id = f.object_id) = 1;
    IF @ForeignKey IS NULL
        OR (SELECT COUNT(*) FROM sys.foreign_keys WHERE referenced_object_id = @Category) <> 1
        THROW 51000, 'Unexpected category foreign keys. Adapt the migration to this schema before execution.', 1;

    DECLARE @CategoryIndex sysname, @CategoryIndexId int, @IndexDescending bit;
    SELECT @CategoryIndex = i.name, @CategoryIndexId = i.index_id, @IndexDescending = c.is_descending_key
    FROM sys.indexes i
    JOIN sys.index_columns c ON c.object_id = i.object_id AND c.index_id = i.index_id
    WHERE i.object_id = @PostCategory AND c.column_id = @CategoryId
        AND i.type = 2 AND i.is_unique = 0 AND i.has_filter = 0
        AND (SELECT COUNT(*) FROM sys.index_columns
            WHERE object_id = i.object_id AND index_id = i.index_id) = 1;
    IF @CategoryIndex IS NULL
        OR EXISTS (
            SELECT 1 FROM sys.index_columns
            WHERE object_id = @Category AND column_id = @Id AND index_id <> @CategoryPkIndex
                OR object_id = @PostCategory AND column_id = @CategoryId
                    AND index_id NOT IN (@PostCategoryPkIndex, @CategoryIndexId))
        OR EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE parent_object_id IN (@Category, @PostCategory))
        THROW 51000, 'Unexpected category indexes or check constraints. Adapt the migration before execution.', 1;

    DECLARE @Sql nvarchar(max) = N'
        ALTER TABLE dbo.Category ADD IdInt int IDENTITY(100, 1) NOT NULL;
        ALTER TABLE dbo.PostCategory ADD CategoryIdInt int NULL;';
    EXEC sys.sp_executesql @Sql;

    EXEC sys.sp_executesql N'
        UPDATE pc SET CategoryIdInt = c.IdInt
        FROM dbo.PostCategory pc JOIN dbo.Category c ON c.Id = pc.CategoryId;
        IF EXISTS (SELECT 1 FROM dbo.PostCategory WHERE CategoryIdInt IS NULL)
            THROW 51000, ''Unmapped article category. Repair orphaned associations before migration.'', 1;';

    SET @Sql = N'ALTER TABLE dbo.PostCategory DROP CONSTRAINT ' + QUOTENAME(@ForeignKey) + N';
        ALTER TABLE dbo.PostCategory DROP CONSTRAINT ' + QUOTENAME(@PostCategoryPk) + N';
        ALTER TABLE dbo.Category DROP CONSTRAINT ' + QUOTENAME(@CategoryPk) + N';
        DROP INDEX ' + QUOTENAME(@CategoryIndex) + N' ON dbo.PostCategory;';
    EXEC sys.sp_executesql @Sql;

    DECLARE @Default sysname;
    SELECT @Default = name FROM sys.default_constraints
        WHERE parent_object_id = @Category AND parent_column_id = @Id;
    IF @Default IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.Category DROP CONSTRAINT ' + QUOTENAME(@Default) + N';';
        EXEC sys.sp_executesql @Sql;
    END;
    SET @Default = NULL;
    SELECT @Default = name FROM sys.default_constraints
        WHERE parent_object_id = @PostCategory AND parent_column_id = @CategoryId;
    IF @Default IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.PostCategory DROP CONSTRAINT ' + QUOTENAME(@Default) + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.PostCategory DROP COLUMN CategoryId;
        ALTER TABLE dbo.Category DROP COLUMN Id;
        ALTER TABLE dbo.PostCategory ALTER COLUMN CategoryIdInt int NOT NULL;';
    EXEC sys.sp_rename N'dbo.Category.IdInt', N'Id', N'COLUMN';
    EXEC sys.sp_rename N'dbo.PostCategory.CategoryIdInt', N'CategoryId', N'COLUMN';

    SET @Sql = N'ALTER TABLE dbo.Category ADD CONSTRAINT ' + QUOTENAME(@CategoryPk)
        + N' PRIMARY KEY ' + @CategoryPkType + N' (Id);
        ALTER TABLE dbo.PostCategory ADD CONSTRAINT ' + QUOTENAME(@PostCategoryPk)
        + N' PRIMARY KEY ' + @PostCategoryPkType + N' (PostId, CategoryId);
        CREATE INDEX ' + QUOTENAME(@CategoryIndex) + N' ON dbo.PostCategory (CategoryId '
        + CASE WHEN @IndexDescending = 1 THEN N'DESC' ELSE N'ASC' END + N');
        ALTER TABLE dbo.PostCategory WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ForeignKey)
        + N' FOREIGN KEY (CategoryId) REFERENCES dbo.Category (Id) ON DELETE ' + @DeleteAction
        + N' ON UPDATE ' + @UpdateAction + N';';
    EXEC sys.sp_executesql @Sql;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
