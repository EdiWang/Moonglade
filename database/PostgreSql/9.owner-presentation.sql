-- Run after 8.owner-avatar.sql with all application hosts stopped and a verified backup.
BEGIN;
LOCK TABLE public.blog_configuration, public.owner_avatar IN SHARE ROW EXCLUSIVE MODE;

DO $upgrade$
DECLARE
    general json;
    names json[];
    descriptions json[];
    owner_name text;
    owner_description text;
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'public' AND table_name = 'owner_avatar' AND column_name = 'name')
       AND EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_schema = 'public' AND table_name = 'owner_avatar' AND column_name = 'description') THEN
        RETURN;
    END IF;

    SELECT cfg_value::json INTO general FROM public.blog_configuration WHERE cfg_key = 'GeneralSettings';
    IF FOUND AND (general IS NULL OR json_typeof(general) <> 'object') THEN
        RAISE EXCEPTION 'GeneralSettings must contain a JSON object before the owner profile cutover.';
    END IF;

    SELECT array_agg(value) FILTER (WHERE lower(key COLLATE "C") = 'ownername'),
           array_agg(value) FILTER (WHERE lower(key COLLATE "C") = 'description')
    INTO names, descriptions FROM json_each(general);
    owner_name := names[1] #>> '{}';
    owner_description := descriptions[1] #>> '{}';
    IF cardinality(names) > 1 OR cardinality(descriptions) > 1
       OR (names IS NOT NULL AND (json_typeof(names[1]) <> 'string' OR owner_name !~ '[^[:space:]]'
           OR length(regexp_replace(owner_name, U&'[\+010000-\+10FFFF]', 'xx', 'g')) > 32))
       OR (descriptions IS NOT NULL AND (json_typeof(descriptions[1]) <> 'string' OR owner_description !~ '[^[:space:]]'
           OR length(regexp_replace(owner_description, U&'[\+010000-\+10FFFF]', 'xx', 'g')) > 256)) THEN
        RAISE EXCEPTION 'Resolve invalid or duplicate owner name and description fields before the cutover.';
    END IF;

    ALTER TABLE public.owner_avatar ADD COLUMN name varchar(32), ADD COLUMN description varchar(256);
    IF names IS NOT NULL OR descriptions IS NOT NULL THEN
        INSERT INTO public.owner_avatar (id, use_gravatar, name, description)
        VALUES (1, false, owner_name, owner_description)
        ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, description = EXCLUDED.description;

        UPDATE public.blog_configuration SET cfg_value = (
            SELECT coalesce(json_object_agg(key, value ORDER BY ordinality), '{}'::json)::text
            FROM json_each(general) WITH ORDINALITY
            WHERE lower(key COLLATE "C") NOT IN ('ownername', 'description')
        ) WHERE cfg_key = 'GeneralSettings';
    END IF;
END
$upgrade$;

COMMIT;
