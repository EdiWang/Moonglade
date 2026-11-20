-- SQL Server: widget-composer release, after 11.owner-oidc-binding-only.sql.
-- Source: dbo.Widget.DisplayOrder. Target: Region/SortOrder and 128-character titles.
-- Back up the database and stop every application host before running this script.
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Widget', N'U') IS NULL
        THROW 50001, 'Missing dbo.Widget; initialize a fresh database with the application instead.', 1;

    -- Hold the source stable, including an empty collection, until schema conversion commits.
    DECLARE @RowCount bigint;
    SELECT @RowCount = COUNT_BIG(*) FROM dbo.Widget WITH (TABLOCKX, HOLDLOCK);

    IF COL_LENGTH(N'dbo.Widget', N'DisplayOrder') IS NOT NULL
        AND COL_LENGTH(N'dbo.Widget', N'Region') IS NULL
        AND COL_LENGTH(N'dbo.Widget', N'SortOrder') IS NULL
    BEGIN
        EXEC sys.sp_executesql N'
            DECLARE @InvalidId uniqueidentifier;
            SELECT TOP (1) @InvalidId = Id FROM dbo.Widget
            WHERE DisplayOrder IS NULL OR DisplayOrder < -30 OR DisplayOrder > 999 OR DATALENGTH(Title) > 256;
            IF @InvalidId IS NOT NULL
            BEGIN
                DECLARE @Message nvarchar(2048) = N''Widget '' + CONVERT(nvarchar(36), @InvalidId)
                    + N'' has an invalid legacy order or an oversized title; no conversion was applied.'';
                THROW 50002, @Message, 1;
            END;';

        ALTER TABLE dbo.Widget ADD Region nvarchar(32) NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.Widget SET Region = CASE
                WHEN DisplayOrder <= -21 THEN N''BeforeProfile''
                WHEN DisplayOrder <= -11 THEN N''BeforeTags''
                WHEN DisplayOrder <= -1 THEN N''BeforeCategories''
                ELSE N''AfterCategories''
            END;

            WITH Ranked AS (
                SELECT Id, CONVERT(int, ROW_NUMBER() OVER (
                    PARTITION BY Region
                    ORDER BY DisplayOrder, CreatedTimeUtc, LOWER(CONVERT(char(36), Id)) COLLATE Latin1_General_100_BIN2
                ) - 1) AS NewOrder
                FROM dbo.Widget
            )
            UPDATE Widget SET DisplayOrder = Ranked.NewOrder
            FROM dbo.Widget AS Widget INNER JOIN Ranked ON Widget.Id = Ranked.Id;';

        EXEC sys.sp_rename N'dbo.Widget.DisplayOrder', N'SortOrder', N'COLUMN';
        ALTER TABLE dbo.Widget ALTER COLUMN Region nvarchar(32) NOT NULL;
        IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Widget'), N'Title', 'AllowsNull') = 1
            ALTER TABLE dbo.Widget ALTER COLUMN Title nvarchar(128) NULL;
        ELSE
            ALTER TABLE dbo.Widget ALTER COLUMN Title nvarchar(128) NOT NULL;
    END
    ELSE IF COL_LENGTH(N'dbo.Widget', N'DisplayOrder') IS NOT NULL
        OR COL_LENGTH(N'dbo.Widget', N'Region') IS NULL
        OR COL_LENGTH(N'dbo.Widget', N'SortOrder') IS NULL
        THROW 50003, 'Incomplete widget region/order schema; restore or review it before retrying.', 1;

    IF ISNULL(COL_LENGTH(N'dbo.Widget', N'Title'), -1) <> 256
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Widget') AND name = N'Title' AND TYPE_NAME(system_type_id) = N'nvarchar')
        OR ISNULL(COL_LENGTH(N'dbo.Widget', N'Region'), -1) <> 64
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Widget') AND name = N'Region' AND TYPE_NAME(system_type_id) = N'nvarchar')
        OR COLUMNPROPERTY(OBJECT_ID(N'dbo.Widget'), N'Region', 'AllowsNull') <> 0
        OR COLUMNPROPERTY(OBJECT_ID(N'dbo.Widget'), N'SortOrder', 'AllowsNull') <> 0
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Widget') AND name = N'SortOrder' AND TYPE_NAME(system_type_id) = N'int')
        THROW 50004, 'Unexpected widget target column types or nullability; no repair was applied.', 1;

    EXEC sys.sp_executesql N'
        IF EXISTS (SELECT 1 FROM dbo.Widget WHERE Region COLLATE Latin1_General_100_BIN2 NOT IN
            (N''BeforeProfile'', N''BeforeTags'', N''BeforeCategories'', N''AfterCategories'') OR SortOrder < 0)
            THROW 50005, ''Invalid widget region/order values; no repair was applied.'', 1;';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
