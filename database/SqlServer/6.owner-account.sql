-- Preparatory account storage, before the account/setup runtime cutover.
-- Requires the existing dbo Moonglade schema with integer category IDs.
-- Stop all Moonglade hosts and back up first. No settings or credentials are transferred.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.BlogConfiguration', N'U') IS NULL
    THROW 50000, 'The existing Moonglade schema is required before adding owner account storage.', 1;

IF OBJECT_ID(N'dbo.OwnerAccount', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OwnerAccount (
        Id uniqueidentifier NOT NULL,
        Slot int NOT NULL,
        Email nvarchar(254) NOT NULL,
        NormalizedEmail nvarchar(254) NOT NULL,
        Revision uniqueidentifier NOT NULL,
        LocalInitializedUtc datetime2(7) NULL,
        PasswordHash nvarchar(256) NULL,
        PasswordSalt nvarchar(128) NULL,
        TotpSecret nvarchar(128) NULL,
        CONSTRAINT PK_OwnerAccount PRIMARY KEY (Id),
        CONSTRAINT CK_OwnerAccount_Slot CHECK (Slot = 1),
        CONSTRAINT CK_OwnerAccount_LocalCredentials CHECK (
            (LocalInitializedUtc IS NULL AND PasswordHash IS NULL AND PasswordSalt IS NULL AND TotpSecret IS NULL)
            OR (LocalInitializedUtc IS NOT NULL AND PasswordHash IS NOT NULL AND PasswordHash <> ''
                AND PasswordSalt IS NOT NULL AND PasswordSalt <> '' AND (TotpSecret IS NULL OR TotpSecret <> '')))
    );
    CREATE UNIQUE INDEX IX_OwnerAccount_Slot ON dbo.OwnerAccount (Slot);
    CREATE UNIQUE INDEX IX_OwnerAccount_NormalizedEmail ON dbo.OwnerAccount (NormalizedEmail);
END;

IF (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'OwnerAccount') <> 9
    OR EXISTS (
        SELECT 1 FROM (VALUES
            (N'Id', N'uniqueidentifier', N'NO', NULL),
            (N'Slot', N'int', N'NO', NULL),
            (N'Email', N'nvarchar', N'NO', 254),
            (N'NormalizedEmail', N'nvarchar', N'NO', 254),
            (N'Revision', N'uniqueidentifier', N'NO', NULL),
            (N'LocalInitializedUtc', N'datetime2', N'YES', NULL),
            (N'PasswordHash', N'nvarchar', N'YES', 256),
            (N'PasswordSalt', N'nvarchar', N'YES', 128),
            (N'TotpSecret', N'nvarchar', N'YES', 128)
        ) expected(Name, Type, Nullable, Length)
        LEFT JOIN INFORMATION_SCHEMA.COLUMNS actual
            ON actual.TABLE_SCHEMA = N'dbo' AND actual.TABLE_NAME = N'OwnerAccount'
            AND actual.COLUMN_NAME = expected.Name
        WHERE actual.COLUMN_NAME IS NULL OR actual.DATA_TYPE <> expected.Type
            OR actual.IS_NULLABLE <> expected.Nullable
            OR ISNULL(actual.CHARACTER_MAXIMUM_LENGTH, -1) <> ISNULL(expected.Length, -1)
            OR (expected.Name = N'LocalInitializedUtc' AND actual.DATETIME_PRECISION <> 7)
    )
    OR NOT EXISTS (SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.OwnerAccount') AND name = N'PK_OwnerAccount' AND type = 'PK')
    OR (SELECT COUNT(*) FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.OwnerAccount') AND is_disabled = 0 AND is_not_trusted = 0
        AND name IN (N'CK_OwnerAccount_Slot', N'CK_OwnerAccount_LocalCredentials')) <> 2
    OR (SELECT COUNT(*) FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OwnerAccount') AND is_unique = 1 AND is_disabled = 0 AND has_filter = 0
        AND name IN (N'IX_OwnerAccount_Slot', N'IX_OwnerAccount_NormalizedEmail')) <> 2
    THROW 50001, 'Owner account storage has an unsupported or incomplete schema. No existing data was changed.', 1;

COMMIT TRANSACTION;
