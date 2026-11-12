-- Run after the owner account cutover and PostgreSQL naming upgrade.
-- Stop all Moonglade hosts and back up the database before running.
BEGIN;
LOCK TABLE public.blog_configuration, public.blog_asset IN SHARE ROW EXCLUSIVE MODE;

DO $upgrade$
DECLARE
    legacy_id constant uuid := '0922e4dc-b47b-44e2-a493-05d4e624381c';
    general json;
    preferences json[];
    enabled boolean := false;
    image text;
BEGIN
    IF to_regclass('public.owner_avatar') IS NOT NULL THEN
        RETURN;
    END IF;

    SELECT cfg_value::json INTO general FROM public.blog_configuration WHERE cfg_key = 'GeneralSettings';
    IF FOUND AND (general IS NULL OR json_typeof(general) <> 'object') THEN
        RAISE EXCEPTION 'GeneralSettings must be a JSON object before the owner avatar cutover.';
    END IF;

    SELECT array_agg(value) INTO preferences FROM json_each(general)
    WHERE lower(key COLLATE "C") = 'usegravatarasprofilepicture';
    IF cardinality(preferences) > 1 OR (preferences IS NOT NULL AND json_typeof(preferences[1]) <> 'boolean') THEN
        RAISE EXCEPTION 'Resolve the invalid Gravatar preference before the owner avatar cutover.';
    END IF;
    IF preferences IS NOT NULL THEN
        enabled := (preferences[1] #>> '{}')::boolean;
    END IF;

    CREATE TABLE public.owner_avatar (
        id integer PRIMARY KEY,
        use_gravatar boolean NOT NULL,
        image_base64 text NULL,
        CONSTRAINT "CK_OwnerAvatar_Slot" CHECK (id = 1)
    );

    SELECT base64data INTO image FROM public.blog_asset WHERE id = legacy_id;
    IF enabled OR image IS NOT NULL THEN
        INSERT INTO public.owner_avatar (id, use_gravatar, image_base64) VALUES (1, enabled, image);
    END IF;

    IF EXISTS (SELECT 1 FROM json_each(general) WHERE lower(key COLLATE "C") IN ('usegravatarasprofilepicture', 'avatarurl')) THEN
        UPDATE public.blog_configuration SET cfg_value = (
            SELECT coalesce(json_object_agg(key, value ORDER BY ordinality), '{}'::json)::text
            FROM json_each(general) WITH ORDINALITY
            WHERE lower(key COLLATE "C") NOT IN ('usegravatarasprofilepicture', 'avatarurl')
        ) WHERE cfg_key = 'GeneralSettings';
    END IF;

    DELETE FROM public.blog_asset WHERE id = legacy_id;
END
$upgrade$;

COMMIT;
