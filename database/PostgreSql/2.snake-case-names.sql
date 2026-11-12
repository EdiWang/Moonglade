-- PostgreSQL: upgrade the standard public schema to snake_case table and column names.
-- Target: the release introducing PostgreSQL snake_case mappings.
-- Prerequisite: apply 1.category-int-identity.sql first if category IDs are still UUIDs.
-- Stop the application and back up the database before running this script.
-- Names of constraints, indexes, and sequences are retained; their dependencies follow renames.
BEGIN;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '5min';

DO $migration$
DECLARE
    names record;
    column_info record;
    old_table oid;
    new_table oid;
    table_id oid;
    source_name text;
    target_column text;
BEGIN
    table_id := COALESCE(to_regclass('public."Category"'), to_regclass('public.category'));
    IF table_id IS NULL OR NOT EXISTS (
        SELECT 1 FROM pg_attribute
        WHERE attrelid = table_id AND attname IN ('Id', 'id')
            AND atttypid = 'integer'::regtype AND NOT attisdropped
    ) THEN
        RAISE EXCEPTION 'This upgrade requires integer category IDs; apply 1.category-int-identity.sql first.';
    END IF;

    FOR names IN SELECT * FROM (VALUES
        ('ActivityLog', 'activity_log'),
        ('BlogAsset', 'blog_asset'),
        ('BlogConfiguration', 'blog_configuration'),
        ('BlogPage', 'blog_page'),
        ('BlogTheme', 'blog_theme'),
        ('Category', 'category'),
        ('Comment', 'comment'),
        ('CommentReply', 'comment_reply'),
        ('EmailOutboxMessage', 'email_outbox_message'),
        ('Mention', 'mention'),
        ('Post', 'post'),
        ('PostCategory', 'post_category'),
        ('PostTag', 'post_tag'),
        ('PostView', 'post_view'),
        ('PostViewDaily', 'post_view_daily'),
        ('SiteVerificationFile', 'site_verification_file'),
        ('StyleSheet', 'style_sheet'),
        ('Tag', 'tag'),
        ('Widget', 'widget')
    ) AS tables(old_name, new_name)
    LOOP
        old_table := to_regclass(format('public.%I', names.old_name));
        new_table := to_regclass(format('public.%I', names.new_name));
        IF old_table IS NOT NULL AND new_table IS NOT NULL THEN
            RAISE EXCEPTION 'Both public.% and public.% exist; resolve the naming conflict before upgrading.',
                names.old_name, names.new_name;
        END IF;
        table_id := COALESCE(old_table, new_table);
        IF table_id IS NULL THEN
            RAISE EXCEPTION 'Required table public.% (or public.%) is missing.', names.old_name, names.new_name;
        END IF;
        source_name := CASE WHEN old_table IS NOT NULL THEN names.old_name ELSE names.new_name END;
        EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE', source_name);

        FOR column_info IN
            SELECT attname FROM pg_attribute
            WHERE attrelid = table_id AND attnum > 0 AND NOT attisdropped
            ORDER BY attnum
        LOOP
            target_column := lower(regexp_replace(regexp_replace(column_info.attname,
                '([A-Z]+)([A-Z][a-z])', '\1_\2', 'g'), '([a-z])([A-Z])', '\1_\2', 'g'));
            IF target_column = column_info.attname THEN CONTINUE; END IF;
            IF EXISTS (SELECT 1 FROM pg_attribute
                WHERE attrelid = table_id AND attname = target_column AND NOT attisdropped) THEN
                RAISE EXCEPTION 'Column naming conflict in public.%: % and %.',
                    source_name, column_info.attname, target_column;
            END IF;
            EXECUTE format('ALTER TABLE public.%I RENAME COLUMN %I TO %I',
                source_name, column_info.attname, target_column);
        END LOOP;

        IF old_table IS NOT NULL THEN
            EXECUTE format('ALTER TABLE public.%I RENAME TO %I', source_name, names.new_name);
        END IF;
    END LOOP;
END
$migration$;

COMMIT;
