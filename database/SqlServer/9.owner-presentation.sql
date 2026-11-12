-- Run after 8.owner-avatar.sql with all application hosts stopped and a verified backup.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.OwnerAvatar', N'U') IS NULL
    THROW 50000, 'Run 8.owner-avatar.sql before the owner profile cutover.', 1;

IF COL_LENGTH(N'dbo.OwnerAvatar', N'Name') IS NULL OR COL_LENGTH(N'dbo.OwnerAvatar', N'Description') IS NULL
BEGIN
    DECLARE @general nvarchar(max) = (
        SELECT CfgValue FROM dbo.BlogConfiguration WITH (UPDLOCK, HOLDLOCK)
        WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'GeneralSettings'
          AND DATALENGTH(CfgKey) = DATALENGTH(N'GeneralSettings'));
    DECLARE @whitespace nvarchar(8) = N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13);
    DECLARE @first int = PATINDEX(N'%[^' + @whitespace + N']%', COALESCE(@general, N''));
    IF @general IS NOT NULL AND (ISJSON(@general) <> 1 OR SUBSTRING(@general, @first, 1) <> N'{')
        THROW 50001, 'GeneralSettings must contain a JSON object before the owner profile cutover.', 1;

    DECLARE @profile TABLE (FieldName nvarchar(4000), FieldKey nvarchar(16), FieldValue nvarchar(max), FieldType int);
    INSERT INTO @profile
    SELECT [key], LOWER([key] COLLATE Latin1_General_100_BIN2), [value], [type] FROM OPENJSON(@general)
    WHERE (LOWER([key] COLLATE Latin1_General_100_BIN2) = N'ownername' AND DATALENGTH([key]) = 18)
       OR (LOWER([key] COLLATE Latin1_General_100_BIN2) = N'description' AND DATALENGTH([key]) = 22);

    IF EXISTS (SELECT 1 FROM @profile GROUP BY FieldKey HAVING COUNT(*) > 1)
       OR EXISTS (SELECT 1 FROM @profile WHERE FieldType <> 1 OR TRIM(@whitespace FROM FieldValue) = N''
                  OR DATALENGTH(FieldValue) > CASE WHEN FieldKey = N'ownername' THEN 64 ELSE 512 END)
        THROW 50002, 'Resolve invalid or duplicate owner name and description fields before the cutover.', 1;

    ALTER TABLE dbo.OwnerAvatar ADD Name nvarchar(32) NULL, Description nvarchar(256) NULL;

    IF EXISTS (SELECT 1 FROM @profile)
    BEGIN
        DECLARE @name nvarchar(32) = (SELECT FieldValue FROM @profile WHERE FieldKey = N'ownername');
        DECLARE @description nvarchar(256) = (SELECT FieldValue FROM @profile WHERE FieldKey = N'description');
        EXEC sys.sp_executesql N'
            UPDATE dbo.OwnerAvatar WITH (UPDLOCK, HOLDLOCK)
            SET Name = @name, Description = @description WHERE Id = 1;
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.OwnerAvatar (Id, UseGravatar, Name, Description) VALUES (1, 0, @name, @description);',
            N'@name nvarchar(32), @description nvarchar(256)', @name, @description;

        DECLARE @nameKey nvarchar(16) = (SELECT FieldName FROM @profile WHERE FieldKey = N'ownername');
        DECLARE @descriptionKey nvarchar(16) = (SELECT FieldName FROM @profile WHERE FieldKey = N'description');
        IF @nameKey IS NOT NULL SET @general = JSON_MODIFY(@general, N'$."' + @nameKey + N'"', NULL);
        IF @descriptionKey IS NOT NULL SET @general = JSON_MODIFY(@general, N'$."' + @descriptionKey + N'"', NULL);
        UPDATE dbo.BlogConfiguration SET CfgValue = @general
        WHERE CfgKey COLLATE Latin1_General_100_BIN2 = N'GeneralSettings'
          AND DATALENGTH(CfgKey) = DATALENGTH(N'GeneralSettings');
    END;
END;

COMMIT TRANSACTION;
