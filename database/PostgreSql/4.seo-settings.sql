-- Run with all Moonglade hosts stopped after the PostgreSQL naming upgrade.
BEGIN;
LOCK TABLE public.blog_configuration IN SHARE ROW EXCLUSIVE MODE;

DO $upgrade$
DECLARE
    general json;
    advanced json;
    seo json;
    duplicate_keys boolean;
BEGIN
    IF EXISTS (SELECT 1 FROM public.blog_configuration WHERE cfg_key = 'SEOSettings') THEN
        RETURN;
    END IF;

    BEGIN
        SELECT cfg_value::json INTO general FROM public.blog_configuration WHERE cfg_key = 'GeneralSettings';
        SELECT cfg_value::json INTO advanced FROM public.blog_configuration WHERE cfg_key = 'AdvancedSettings';
    EXCEPTION WHEN invalid_text_representation THEN
        RAISE EXCEPTION 'GeneralSettings and AdvancedSettings must contain valid JSON objects before the SEO upgrade.';
    END;

    IF json_typeof(general) <> 'object' OR json_typeof(advanced) <> 'object' THEN
        RAISE EXCEPTION 'GeneralSettings and AdvancedSettings must contain valid JSON objects before the SEO upgrade.';
    END IF;

    WITH fields(group_name, name) AS (
        VALUES ('GeneralSettings', 'canonicalPrefix'),
               ('GeneralSettings', 'metaKeyword'),
               ('GeneralSettings', 'useDublinCoreMetaData'),
               ('GeneralSettings', 'dcLicenseUrl'),
               ('AdvancedSettings', 'enableSiteMap'),
               ('AdvancedSettings', 'enableJsonLdBlogPosting')
    ), legacy_values AS (
        SELECT 'GeneralSettings' AS group_name, key, value FROM json_each(general)
        UNION ALL
        SELECT 'AdvancedSettings', key, value FROM json_each(advanced)
    )
    SELECT COALESCE(json_object_agg(fields.name, legacy_values.value), '{}'::json),
           COUNT(*) <> COUNT(DISTINCT fields.name)
    INTO seo, duplicate_keys
    FROM fields JOIN legacy_values
        ON legacy_values.group_name = fields.group_name AND lower(legacy_values.key COLLATE "C") = lower(fields.name COLLATE "C");

    IF duplicate_keys THEN
        RAISE EXCEPTION 'Resolve duplicate SEO fields in GeneralSettings or AdvancedSettings before running this upgrade.';
    END IF;

    INSERT INTO public.blog_configuration (cfg_key, cfg_value, last_modified_time_utc)
    VALUES ('SEOSettings', seo::text, CURRENT_TIMESTAMP);
END
$upgrade$;

COMMIT;
