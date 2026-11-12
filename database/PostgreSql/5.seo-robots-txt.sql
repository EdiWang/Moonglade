-- Run after 4.seo-settings.sql with all Moonglade hosts stopped.
BEGIN;
LOCK TABLE public.blog_configuration IN SHARE ROW EXCLUSIVE MODE;

DO $upgrade$
DECLARE
    seo json;
    advanced json;
    legacy_values json[];
    robots json;
BEGIN
    BEGIN
        SELECT cfg_value::json INTO seo FROM public.blog_configuration WHERE cfg_key = 'SEOSettings';
    EXCEPTION WHEN invalid_text_representation THEN
        RAISE EXCEPTION 'SEOSettings must contain a valid JSON object before the robots.txt upgrade.';
    END;

    IF seo IS NULL OR json_typeof(seo) <> 'object' THEN
        RAISE EXCEPTION 'A valid SEOSettings row is required. Run 4.seo-settings.sql first if the row is missing.';
    END IF;

    IF EXISTS (SELECT 1 FROM json_each(seo) WHERE lower(key COLLATE "C") = 'robotstxtcontent') THEN
        RETURN;
    END IF;

    BEGIN
        SELECT cfg_value::json INTO advanced FROM public.blog_configuration WHERE cfg_key = 'AdvancedSettings';
    EXCEPTION WHEN invalid_text_representation THEN
        RAISE EXCEPTION 'AdvancedSettings must contain a valid JSON object before the robots.txt upgrade.';
    END;

    IF json_typeof(advanced) <> 'object' THEN
        RAISE EXCEPTION 'AdvancedSettings must contain a valid JSON object before the robots.txt upgrade.';
    END IF;

    SELECT array_agg(value) INTO legacy_values
    FROM json_each(advanced)
    WHERE lower(key COLLATE "C") = 'robotstxtcontent';

    IF legacy_values IS NULL THEN
        RETURN;
    END IF;

    robots := legacy_values[1];
    IF cardinality(legacy_values) <> 1 OR json_typeof(robots) NOT IN ('string', 'null')
       OR length(regexp_replace(robots #>> '{}', U&'[\+010000-\+10FFFF]', 'xx', 'g')) > 1024 THEN
        RAISE EXCEPTION 'Resolve the invalid or duplicate robotsTxtContent in AdvancedSettings before running this upgrade.';
    END IF;

    SELECT json_object_agg(member.key, member.value ORDER BY member.ordinality) INTO seo
    FROM (
        SELECT * FROM json_each(seo) WITH ORDINALITY
        UNION ALL
        SELECT 'robotsTxtContent', robots, NULL::bigint
    ) AS member;

    UPDATE public.blog_configuration
    SET cfg_value = seo::text, last_modified_time_utc = CURRENT_TIMESTAMP
    WHERE cfg_key = 'SEOSettings';
END
$upgrade$;

COMMIT;
