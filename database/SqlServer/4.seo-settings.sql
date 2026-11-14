-- Run with all Moonglade hosts stopped before starting the upgraded application.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.BlogConfiguration WITH (UPDLOCK, HOLDLOCK) WHERE CfgKey = N'SEOSettings')
BEGIN
    DECLARE @general nvarchar(max) = (SELECT CfgValue FROM dbo.BlogConfiguration WHERE CfgKey = N'GeneralSettings');
    DECLARE @advanced nvarchar(max) = (SELECT CfgValue FROM dbo.BlogConfiguration WHERE CfgKey = N'AdvancedSettings');

    IF ISJSON(@general, OBJECT) = 0 OR ISJSON(@advanced, OBJECT) = 0
        THROW 50000, 'GeneralSettings and AdvancedSettings must contain valid JSON objects before the SEO upgrade.', 1;

    DECLARE @settings TABLE (FieldName nvarchar(64), JsonValue nvarchar(max), JsonType int);

    WITH fields AS (
        SELECT GroupName, FieldName FROM (VALUES
            (N'GeneralSettings', N'canonicalPrefix'),
            (N'GeneralSettings', N'metaKeyword'),
            (N'GeneralSettings', N'useDublinCoreMetaData'),
            (N'GeneralSettings', N'dcLicenseUrl'),
            (N'AdvancedSettings', N'enableSiteMap'),
            (N'AdvancedSettings', N'enableJsonLdBlogPosting')
        ) AS field_map(GroupName, FieldName)
    ), legacy_values AS (
        SELECT N'GeneralSettings' AS GroupName, [key], [value], [type] FROM OPENJSON(@general)
        UNION ALL
        SELECT N'AdvancedSettings', [key], [value], [type] FROM OPENJSON(@advanced)
    )
    INSERT INTO @settings (FieldName, JsonValue, JsonType)
    SELECT fields.FieldName, legacy_values.[value], legacy_values.[type]
    FROM fields JOIN legacy_values ON legacy_values.GroupName = fields.GroupName
        AND LOWER(legacy_values.[key] COLLATE Latin1_General_100_BIN2) = LOWER(fields.FieldName COLLATE Latin1_General_100_BIN2)
        AND DATALENGTH(legacy_values.[key]) = DATALENGTH(fields.FieldName);

    IF EXISTS (SELECT 1 FROM @settings GROUP BY FieldName HAVING COUNT(*) > 1)
        THROW 50001, 'Resolve duplicate SEO fields in GeneralSettings or AdvancedSettings before running this upgrade.', 1;

    DECLARE @seo nvarchar(max);
    SELECT @seo = N'{' + COALESCE(STRING_AGG(N'"' + FieldName + N'":' +
        CASE JsonType
            WHEN 0 THEN N'null'
            WHEN 1 THEN N'"' + STRING_ESCAPE(JsonValue, 'json') + N'"'
            ELSE JsonValue
        END, N','), N'') + N'}'
    FROM @settings;

    INSERT INTO dbo.BlogConfiguration (CfgKey, CfgValue, LastModifiedTimeUtc)
    VALUES (N'SEOSettings', @seo, SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
