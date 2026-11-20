-- PostgreSQL: widget-composer release, after 11.owner-oidc-binding-only.sql.
-- Source: legacy widget.display_order. Target: region/sort_order and 128-character titles.
-- Back up the database and stop every application host before running this script.
BEGIN;

DO $$
DECLARE
    has_legacy boolean;
    has_region boolean;
    has_sort_order boolean;
    invalid_id uuid;
BEGIN
    IF to_regclass('public.widget') IS NULL THEN
        RAISE EXCEPTION 'Missing public.widget; initialize a fresh database with the application instead.';
    END IF;

    LOCK TABLE public.widget IN ACCESS EXCLUSIVE MODE;
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'display_order') INTO has_legacy;
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'region') INTO has_region;
    SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'sort_order') INTO has_sort_order;

    IF has_legacy AND NOT has_region AND NOT has_sort_order THEN
        SELECT id INTO invalid_id FROM public.widget
        WHERE display_order IS NULL OR display_order < -30 OR display_order > 999 OR char_length(title) > 128
        LIMIT 1;
        IF invalid_id IS NOT NULL THEN
            RAISE EXCEPTION 'Widget % has an invalid legacy order or an oversized title; no conversion was applied.', invalid_id;
        END IF;

        ALTER TABLE public.widget ADD COLUMN region character varying(32);
        UPDATE public.widget SET region = CASE
            WHEN display_order <= -21 THEN 'BeforeProfile'
            WHEN display_order <= -11 THEN 'BeforeTags'
            WHEN display_order <= -1 THEN 'BeforeCategories'
            ELSE 'AfterCategories'
        END;

        WITH ranked AS (
            SELECT id, (row_number() OVER (
                PARTITION BY region
                ORDER BY display_order, created_time_utc, lower(id::text) COLLATE "C"
            ) - 1)::integer AS new_order
            FROM public.widget
        )
        UPDATE public.widget AS widget SET display_order = ranked.new_order
        FROM ranked WHERE widget.id = ranked.id;

        ALTER TABLE public.widget RENAME COLUMN display_order TO sort_order;
        ALTER TABLE public.widget ALTER COLUMN region SET NOT NULL;
        ALTER TABLE public.widget ALTER COLUMN title TYPE character varying(128);
    ELSIF has_legacy OR NOT has_region OR NOT has_sort_order THEN
        RAISE EXCEPTION 'Incomplete widget region/order schema; restore or review it before retrying.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'title' AND data_type = 'character varying' AND character_maximum_length = 128)
        OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'region' AND data_type = 'character varying' AND character_maximum_length = 32 AND is_nullable = 'NO')
        OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'sort_order' AND data_type = 'integer' AND is_nullable = 'NO') THEN
        RAISE EXCEPTION 'Unexpected widget target column types or nullability; no repair was applied.';
    END IF;

    IF EXISTS (SELECT 1 FROM public.widget WHERE region COLLATE "C" NOT IN ('BeforeProfile', 'BeforeTags', 'BeforeCategories', 'AfterCategories') OR sort_order < 0) THEN
        RAISE EXCEPTION 'Invalid widget region/order values; no repair was applied.';
    END IF;
END $$;

COMMIT;
