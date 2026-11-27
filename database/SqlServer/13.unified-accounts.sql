-- SQL Server unified-account upgrade, after 12.widget-regions.sql.
-- Back up the database and stop every application host before running this script.
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.OwnerOidc', N'U') IS NULL OR OBJECT_ID(N'dbo.DataProtectionKeys', N'U') IS NULL
        OR OBJECT_ID(N'dbo.ActivityLog', N'U') IS NULL OR COL_LENGTH(N'dbo.Widget', N'Region') IS NULL
        THROW 50001, 'Apply the required Moonglade upgrades through script 12 first.', 1;

    IF OBJECT_ID(N'dbo.OwnerAccount', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.BlogAccount', N'U') IS NULL
    BEGIN
        -- Renaming the table preserves the existing rows and the OIDC foreign key.
        EXEC sys.sp_rename N'dbo.OwnerAccount', N'BlogAccount', N'OBJECT';
        EXEC sys.sp_rename N'dbo.PK_OwnerAccount', N'PK_BlogAccount', N'OBJECT';
        EXEC sys.sp_rename N'dbo.CK_OwnerAccount_LocalCredentials', N'CK_BlogAccount_LocalCredentials', N'OBJECT';
        EXEC sys.sp_rename N'dbo.BlogAccount.IX_OwnerAccount_Slot', N'IX_BlogAccount_Slot', N'INDEX';
        EXEC sys.sp_rename N'dbo.BlogAccount.IX_OwnerAccount_NormalizedEmail', N'IX_BlogAccount_NormalizedEmail', N'INDEX';

        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.BlogAccount DROP CONSTRAINT CK_OwnerAccount_Slot;
            ALTER TABLE dbo.BlogAccount ADD
                DisplayName nvarchar(100) NULL,
                IsDisabled bit NOT NULL CONSTRAINT DF_BlogAccount_IsDisabled DEFAULT (0),
                MustChangePassword bit NOT NULL CONSTRAINT DF_BlogAccount_MustChangePassword DEFAULT (0);';
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.BlogAccount WITH CHECK ADD CONSTRAINT CK_BlogAccount_SlotState CHECK (
                (Slot = 1 AND IsDisabled = 0 AND MustChangePassword = 0)
                OR (Slot BETWEEN 2 AND 3 AND LocalInitializedUtc IS NOT NULL));';
    END
    ELSE IF OBJECT_ID(N'dbo.OwnerAccount', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.BlogAccount', N'U') IS NULL
        THROW 50002, 'Unexpected Moonglade account tables. Review the database before upgrading.', 1;

    DECLARE @AccountTable int = OBJECT_ID(N'dbo.BlogAccount', N'U');

    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = @AccountTable AND name = N'PK_BlogAccount' AND type = 'PK')
        OR (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id = @AccountTable
            AND is_disabled = 0 AND is_not_trusted = 0
            AND name IN (N'CK_BlogAccount_LocalCredentials', N'CK_BlogAccount_SlotState')) <> 2
        OR (SELECT COUNT(*) FROM sys.indexes WHERE object_id = @AccountTable
            AND is_unique = 1 AND is_disabled = 0 AND has_filter = 0
            AND name IN (N'IX_BlogAccount_Slot', N'IX_BlogAccount_NormalizedEmail')) <> 2
        OR NOT EXISTS (SELECT 1 FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.OwnerOidc') AND referenced_object_id = @AccountTable
                AND is_disabled = 0 AND is_not_trusted = 0)
        THROW 50003, 'The unified account schema is incomplete. Review the database before upgrading.', 1;

    IF COL_LENGTH(N'dbo.ActivityLog', N'ActorAccountId') IS NULL
        ALTER TABLE dbo.ActivityLog ADD ActorAccountId uniqueidentifier NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
