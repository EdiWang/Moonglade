-- Run with all hosts stopped after the owner account schema upgrade.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.OwnerOidc', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OwnerOidc (
        Id int NOT NULL CONSTRAINT PK_OwnerOidc PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL CONSTRAINT FK_OwnerOidc_OwnerAccount_OwnerId
            REFERENCES dbo.OwnerAccount (Id) ON DELETE CASCADE,
        Revision uniqueidentifier NOT NULL,
        Enabled bit NOT NULL,
        DisplayName nvarchar(100) NOT NULL,
        Authority nvarchar(500) NOT NULL,
        ClientId nvarchar(500) NOT NULL,
        ProtectedSecret nvarchar(max) NOT NULL,
        Issuer nvarchar(500) NULL,
        Subject nvarchar(255) NULL,
        AccountLabel nvarchar(255) NULL,
        SessionRevision uniqueidentifier NULL,
        CONSTRAINT CK_OwnerOidc_Slot CHECK (Id = 1)
    );
    CREATE UNIQUE INDEX IX_OwnerOidc_OwnerId ON dbo.OwnerOidc (OwnerId);
END;

IF OBJECT_ID(N'dbo.DataProtectionKeys', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataProtectionKeys (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_DataProtectionKeys PRIMARY KEY,
        FriendlyName nvarchar(max) NULL,
        Xml nvarchar(max) NULL
    );
END;

COMMIT TRANSACTION;
