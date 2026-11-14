-- Run after 4.seo-settings.sql with all Moonglade hosts stopped.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @seo nvarchar(max) = (SELECT CfgValue FROM dbo.BlogConfiguration WITH (UPDLOCK, HOLDLOCK) WHERE CfgKey = N'SEOSettings');

IF ISNULL(ISJSON(@seo, OBJECT), 0) = 0
    THROW 50000, 'A valid SEOSettings row is required. Run 4.seo-settings.sql first if the row is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM OPENJSON(@seo)
    WHERE LOWER([key] COLLATE Latin1_General_100_BIN2) = N'robotstxtcontent'
      AND DATALENGTH([key]) = DATALENGTH(N'robotsTxtContent')
)
BEGIN
    DECLARE @advanced nvarchar(max) = (SELECT CfgValue FROM dbo.BlogConfiguration WITH (HOLDLOCK) WHERE CfgKey = N'AdvancedSettings');

    IF ISJSON(@advanced, OBJECT) = 0
        THROW 50001, 'AdvancedSettings must contain a valid JSON object before the robots.txt upgrade.', 1;

    DECLARE @legacy TABLE (Content nvarchar(max), JsonType int);
    INSERT INTO @legacy (Content, JsonType)
    SELECT [value], [type] FROM OPENJSON(@advanced)
    WHERE LOWER([key] COLLATE Latin1_General_100_BIN2) = N'robotstxtcontent'
      AND DATALENGTH([key]) = DATALENGTH(N'robotsTxtContent');

    IF (SELECT COUNT(*) FROM @legacy) > 1
       OR EXISTS (SELECT 1 FROM @legacy WHERE JsonType NOT IN (0, 1) OR DATALENGTH(Content) > 2048)
        THROW 50002, 'Resolve the invalid or duplicate robotsTxtContent in AdvancedSettings before running this upgrade.', 1;

    IF EXISTS (SELECT 1 FROM @legacy)
    BEGIN
        DECLARE @robots nvarchar(max) = (SELECT Content FROM @legacy);
        SET @seo = JSON_MODIFY(@seo, '$.robotsTxtContent', COALESCE(@robots, N''));
        IF @robots IS NULL
            SET @seo = JSON_MODIFY(@seo, 'strict $.robotsTxtContent', NULL);

        UPDATE dbo.BlogConfiguration
        SET CfgValue = @seo, LastModifiedTimeUtc = SYSUTCDATETIME()
        WHERE CfgKey = N'SEOSettings';
    END;
END;

COMMIT TRANSACTION;
