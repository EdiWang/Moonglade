-- SQL Server: upgrade the owner OIDC schema from 16.10.0 to 16.11.0 after 10.owner-oidc.sql.
-- Run with all hosts stopped. Existing bindings, including disabled ones, remain usable.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.OwnerOidc', N'Enabled') IS NOT NULL
    ALTER TABLE dbo.OwnerOidc DROP COLUMN Enabled;

COMMIT TRANSACTION;
