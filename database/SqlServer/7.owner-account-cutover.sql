-- Account/email cutover for the release introducing first-run owner setup.
-- Run 6.owner-account.sql first, after the category identity prerequisite.
-- Stop every host and back up first. No legacy credentials are imported.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.OwnerAccount', N'U') IS NULL OR OBJECT_ID(N'dbo.BlogConfiguration', N'U') IS NULL
    THROW 50000, 'Run 6.owner-account.sql against the standard Moonglade schema before the account cutover.', 1;

DECLARE @hasOwner bit = CASE WHEN EXISTS (
    SELECT 1 FROM dbo.OwnerAccount WITH (UPDLOCK, HOLDLOCK)) THEN 1 ELSE 0 END;
DECLARE @general nvarchar(max) = (
    SELECT CfgValue FROM dbo.BlogConfiguration WITH (UPDLOCK, HOLDLOCK)
    WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'GeneralSettings'
        AND DATALENGTH(CfgKey) = DATALENGTH(N'GeneralSettings'));
DECLARE @first int = PATINDEX(N'%[^ ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + N']%',
    @general COLLATE Latin1_General_100_BIN2);
IF ISNULL(ISJSON(@general), 0) = 0 OR SUBSTRING(@general, @first, 1) <> N'{'
    THROW 50001, 'A GeneralSettings JSON object is required before the owner account cutover.', 1;

DECLARE @addresses TABLE (FieldName nvarchar(4000), Address nvarchar(max), JsonType int);
INSERT INTO @addresses (FieldName, Address, JsonType)
SELECT [key], [value], [type] FROM OPENJSON(@general)
WHERE LOWER([key] COLLATE Latin1_General_100_BIN2) = N'owneremail'
    AND DATALENGTH([key]) = DATALENGTH(N'ownerEmail');
IF (SELECT COUNT(*) FROM @addresses) > 1
    THROW 50002, 'Resolve duplicate OwnerEmail fields before the owner account cutover.', 1;

IF @hasOwner = 0 AND EXISTS (SELECT 1 FROM @addresses)
BEGIN
    DECLARE @email nvarchar(max) = (SELECT TRIM(Address) FROM @addresses);
    IF (SELECT JsonType FROM @addresses) <> 1 OR @email IS NULL OR @email = N''
        OR CHARINDEX(NCHAR(13), @email) > 0 OR CHARINDEX(NCHAR(10), @email) > 0
        OR CHARINDEX(N'@', @email) <= 1 OR CHARINDEX(N'@', @email) >= LEN(@email)
        OR CHARINDEX(N'@', @email, CHARINDEX(N'@', @email) + 1) > 0
        OR DATALENGTH(@email) > 508 OR DATALENGTH(UPPER(@email COLLATE Latin1_General_100_BIN2)) > 508
        THROW 50003, 'Resolve the invalid public OwnerEmail before the account cutover. No credentials were changed.', 1;

    INSERT INTO dbo.OwnerAccount
        (Id, Slot, Email, NormalizedEmail, Revision, LocalInitializedUtc, PasswordHash, PasswordSalt, TotpSecret)
    VALUES
        (NEWID(), 1, @email, UPPER(@email COLLATE Latin1_General_100_BIN2), NEWID(), NULL, NULL, NULL, NULL);
END;

IF EXISTS (SELECT 1 FROM @addresses)
BEGIN
    DECLARE @field nvarchar(4000) = (SELECT FieldName FROM @addresses);
    DECLARE @path nvarchar(4000) = N'$."' + @field + N'"';
    -- Removing only the selected member retains unrelated JSON values and row timestamps.
    SET @general = JSON_MODIFY(@general, @path, NULL);
    UPDATE dbo.BlogConfiguration SET CfgValue = @general
    WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'GeneralSettings'
        AND DATALENGTH(CfgKey) = DATALENGTH(N'GeneralSettings');
END;

DELETE FROM dbo.BlogConfiguration
WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'LocalAccountSettings'
    AND DATALENGTH(CfgKey) = DATALENGTH(N'LocalAccountSettings');
COMMIT TRANSACTION;
