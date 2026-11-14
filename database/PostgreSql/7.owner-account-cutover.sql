-- Account/email cutover for the release introducing first-run owner setup.
-- Run 6.owner-account.sql first, after category and snake_case prerequisites.
-- Stop every host and back up first. No legacy credentials are imported.
BEGIN;
LOCK TABLE public.blog_configuration, public.owner_account IN SHARE ROW EXCLUSIVE MODE;

DO $upgrade$
DECLARE
    general json;
    addresses json[];
    address text;
    cleaned json;
BEGIN
    BEGIN
        SELECT cfg_value::json INTO general
        FROM public.blog_configuration WHERE cfg_key = 'GeneralSettings';
    EXCEPTION WHEN invalid_text_representation THEN
        RAISE EXCEPTION 'GeneralSettings must contain a valid JSON object before the owner account cutover.';
    END;
    IF general IS NULL OR json_typeof(general) <> 'object' THEN
        RAISE EXCEPTION 'A GeneralSettings JSON object is required before the owner account cutover.';
    END IF;

    SELECT array_agg(value) INTO addresses FROM json_each(general)
    WHERE lower(key COLLATE "C") = 'owneremail';
    IF cardinality(addresses) > 1 THEN
        RAISE EXCEPTION 'Resolve duplicate OwnerEmail fields before the owner account cutover.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.owner_account) AND addresses IS NOT NULL THEN
        address := btrim(addresses[1] #>> '{}');
        IF json_typeof(addresses[1]) <> 'string' OR address IS NULL
            OR address = '' OR address ~ E'[\\r\\n]'
            OR address !~ '^[^@]+@[^@]+$'
            OR length(regexp_replace(address, U&'[\+010000-\+10FFFF]', 'xx', 'g')) > 254
            OR length(regexp_replace(upper(address), U&'[\+010000-\+10FFFF]', 'xx', 'g')) > 254 THEN
            RAISE EXCEPTION 'Resolve the invalid public OwnerEmail before the owner account cutover. No credentials were changed.';
        END IF;
        INSERT INTO public.owner_account
            (id, slot, email, normalized_email, revision, local_initialized_utc, password_hash, password_salt, totp_secret)
        VALUES
            (gen_random_uuid(), 1, address, upper(address), gen_random_uuid(), NULL, NULL, NULL, NULL);
    END IF;

    IF addresses IS NOT NULL THEN
        -- Keep unrelated members (including duplicate keys) and their JSON values.
        SELECT coalesce(json_object_agg(key, value ORDER BY ordinality), '{}'::json)
        INTO cleaned FROM json_each(general) WITH ORDINALITY
        WHERE lower(key COLLATE "C") <> 'owneremail';
        UPDATE public.blog_configuration SET cfg_value = cleaned::text
        WHERE cfg_key = 'GeneralSettings';
    END IF;

    DELETE FROM public.blog_configuration WHERE cfg_key = 'LocalAccountSettings';
END
$upgrade$;

COMMIT;
