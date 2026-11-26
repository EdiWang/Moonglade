-- PostgreSQL unified-account upgrade, after 12.widget-regions.sql.
-- Back up the database and stop every application host before running this script.
BEGIN;

DO $upgrade$
BEGIN
    IF to_regclass('public.owner_oidc') IS NULL OR to_regclass('public.data_protection_keys') IS NULL
        OR to_regclass('public.activity_log') IS NULL
        OR NOT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'widget' AND column_name = 'region') THEN
        RAISE EXCEPTION 'Apply the required Moonglade upgrades through script 12 first.';
    END IF;

    IF to_regclass('public.owner_account') IS NOT NULL AND to_regclass('public.blog_account') IS NULL THEN
        -- Renaming the table preserves the existing rows and the OIDC foreign key.
        ALTER TABLE public.owner_account RENAME TO blog_account;
        ALTER TABLE public.blog_account RENAME CONSTRAINT "PK_OwnerAccount" TO "PK_BlogAccount";
        ALTER TABLE public.blog_account RENAME CONSTRAINT "CK_OwnerAccount_LocalCredentials"
            TO "CK_BlogAccount_LocalCredentials";
        ALTER INDEX public."IX_OwnerAccount_Slot" RENAME TO "IX_BlogAccount_Slot";
        ALTER INDEX public."IX_OwnerAccount_NormalizedEmail" RENAME TO "IX_BlogAccount_NormalizedEmail";

        ALTER TABLE public.blog_account
            DROP CONSTRAINT "CK_OwnerAccount_Slot",
            ADD COLUMN display_name character varying(100),
            ADD COLUMN is_disabled boolean NOT NULL DEFAULT false,
            ADD COLUMN must_change_password boolean NOT NULL DEFAULT false;
        ALTER TABLE public.blog_account ADD CONSTRAINT "CK_BlogAccount_SlotState" CHECK (
            (slot = 1 AND is_disabled = false AND must_change_password = false)
            OR (slot BETWEEN 2 AND 3 AND local_initialized_utc IS NOT NULL));
    ELSIF to_regclass('public.owner_account') IS NOT NULL OR to_regclass('public.blog_account') IS NULL THEN
        RAISE EXCEPTION 'Unexpected Moonglade account tables. Review the database before upgrading.';
    END IF;

    IF (SELECT count(*) FROM pg_constraint
        WHERE conrelid = 'public.blog_account'::regclass AND convalidated
            AND conname IN ('PK_BlogAccount', 'CK_BlogAccount_LocalCredentials', 'CK_BlogAccount_SlotState')) <> 3
        OR (SELECT count(*) FROM pg_index AS i JOIN pg_class AS c ON c.oid = i.indexrelid
            WHERE i.indrelid = 'public.blog_account'::regclass AND i.indisunique AND i.indisvalid AND i.indpred IS NULL
                AND c.relname IN ('IX_BlogAccount_Slot', 'IX_BlogAccount_NormalizedEmail')) <> 2
        OR NOT EXISTS (SELECT 1 FROM pg_constraint
            WHERE conrelid = 'public.owner_oidc'::regclass AND confrelid = 'public.blog_account'::regclass
                AND contype = 'f' AND convalidated) THEN
        RAISE EXCEPTION 'The unified account schema is incomplete. Review the database before upgrading.';
    END IF;

    ALTER TABLE public.activity_log ADD COLUMN IF NOT EXISTS actor_account_id uuid;
END $upgrade$;

COMMIT;
