-- Run after the owner account cutover.
-- Stop all Moonglade hosts and back up the database before running.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.OwnerAvatar', N'U') IS NULL
BEGIN
    DECLARE @legacyId uniqueidentifier = '0922e4dc-b47b-44e2-a493-05d4e624381c';
    DECLARE @general nvarchar(max) = (
        SELECT CfgValue FROM dbo.BlogConfiguration
        WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'GeneralSettings'
            AND DATALENGTH(CfgKey) = DATALENGTH(N'GeneralSettings'));
    DECLARE @first int = PATINDEX(N'%[^ ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + N']%', COALESCE(@general, N''));
    IF @general IS NOT NULL AND (ISJSON(@general) <> 1 OR SUBSTRING(@general, @first, 1) <> N'{')
        THROW 50000, 'GeneralSettings must be a JSON object before the owner avatar cutover.', 1;

    DECLARE @preference TABLE (FieldName nvarchar(4000), Enabled nvarchar(max), JsonType int);
    INSERT INTO @preference
    SELECT [key], [value], [type] FROM OPENJSON(@general)
    WHERE LOWER([key] COLLATE Latin1_General_100_BIN2) = N'usegravatarasprofilepicture'
        AND DATALENGTH([key]) = DATALENGTH(N'usegravatarasprofilepicture');
    IF (SELECT COUNT(*) FROM @preference) > 1 OR EXISTS (SELECT 1 FROM @preference WHERE JsonType <> 3)
        THROW 50001, 'Resolve the invalid Gravatar preference before the owner avatar cutover.', 1;

    CREATE TABLE dbo.OwnerAvatar (
        Id int NOT NULL CONSTRAINT PK_OwnerAvatar PRIMARY KEY,
        UseGravatar bit NOT NULL,
        ImageBase64 nvarchar(max) NULL,
        CONSTRAINT CK_OwnerAvatar_Slot CHECK (Id = 1)
    );

    DECLARE @image nvarchar(max) = (SELECT Base64Data FROM dbo.BlogAsset WHERE Id = @legacyId);
    DECLARE @enabled bit = CASE WHEN EXISTS (SELECT 1 FROM @preference WHERE Enabled = N'true') THEN 1 ELSE 0 END;
    IF @enabled = 1 OR @image IS NOT NULL
        INSERT INTO dbo.OwnerAvatar (Id, UseGravatar, ImageBase64) VALUES (1, @enabled, @image);

    DECLARE @field nvarchar(4000) = (SELECT FieldName FROM @preference);
    IF @field IS NOT NULL
        SET @general = JSON_MODIFY(@general, N'$."' + @field + N'"', NULL);

    SET @field = (
        SELECT TOP (1) [key] FROM OPENJSON(@general)
        WHERE LOWER([key] COLLATE Latin1_General_100_BIN2) = N'avatarurl'
            AND DATALENGTH([key]) = DATALENGTH(N'avatarurl'));
    WHILE @field IS NOT NULL
    BEGIN
        SET @general = JSON_MODIFY(@general, N'$."' + @field + N'"', NULL);
        SET @field = (
            SELECT TOP (1) [key] FROM OPENJSON(@general)
            WHERE LOWER([key] COLLATE Latin1_General_100_BIN2) = N'avatarurl'
                AND DATALENGTH([key]) = DATALENGTH(N'avatarurl'));
    END;

    UPDATE dbo.BlogConfiguration SET CfgValue = @general
    WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'GeneralSettings'
        AND DATALENGTH(CfgKey) = DATALENGTH(N'GeneralSettings')
        AND DATALENGTH(CfgValue) <> DATALENGTH(@general);

    DELETE FROM dbo.BlogAsset WHERE Id = @legacyId;
END;

COMMIT TRANSACTION;
