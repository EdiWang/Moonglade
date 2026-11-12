-- Preparatory account storage, before the account/setup runtime cutover.
-- Requires the existing public schema with integer category IDs and snake_case names.
-- Stop all Moonglade hosts and back up first. No settings or credentials are transferred.
BEGIN;

DO $upgrade$
BEGIN
    IF to_regclass('public.blog_configuration') IS NULL THEN
        RAISE EXCEPTION 'The existing snake_case Moonglade schema is required before adding owner account storage.';
    END IF;

    IF to_regclass('public.owner_account') IS NULL THEN
        CREATE TABLE public.owner_account (
            id uuid NOT NULL,
            slot integer NOT NULL,
            email character varying(254) NOT NULL,
            normalized_email character varying(254) NOT NULL,
            revision uuid NOT NULL,
            local_initialized_utc timestamp with time zone NULL,
            password_hash character varying(256) NULL,
            password_salt character varying(128) NULL,
            totp_secret character varying(128) NULL,
            CONSTRAINT "PK_OwnerAccount" PRIMARY KEY (id),
            CONSTRAINT "CK_OwnerAccount_Slot" CHECK (slot = 1),
            CONSTRAINT "CK_OwnerAccount_LocalCredentials" CHECK (
                (local_initialized_utc IS NULL AND password_hash IS NULL AND password_salt IS NULL AND totp_secret IS NULL)
                OR (local_initialized_utc IS NOT NULL AND password_hash IS NOT NULL AND password_hash <> ''
                    AND password_salt IS NOT NULL AND password_salt <> '' AND (totp_secret IS NULL OR totp_secret <> '')))
        );
        CREATE UNIQUE INDEX "IX_OwnerAccount_Slot" ON public.owner_account (slot);
        CREATE UNIQUE INDEX "IX_OwnerAccount_NormalizedEmail" ON public.owner_account (normalized_email);
    END IF;

    IF (SELECT count(*) FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'owner_account') <> 9
        OR EXISTS (
            SELECT 1 FROM (VALUES
                ('id', 'uuid', 'NO', NULL::integer),
                ('slot', 'integer', 'NO', NULL::integer),
                ('email', 'character varying', 'NO', 254),
                ('normalized_email', 'character varying', 'NO', 254),
                ('revision', 'uuid', 'NO', NULL::integer),
                ('local_initialized_utc', 'timestamp with time zone', 'YES', NULL::integer),
                ('password_hash', 'character varying', 'YES', 256),
                ('password_salt', 'character varying', 'YES', 128),
                ('totp_secret', 'character varying', 'YES', 128)
            ) AS expected(name, type, nullable, length)
            LEFT JOIN information_schema.columns actual
                ON actual.table_schema = 'public' AND actual.table_name = 'owner_account'
                AND actual.column_name = expected.name
            WHERE actual.column_name IS NULL OR actual.data_type <> expected.type
                OR actual.is_nullable <> expected.nullable
                OR actual.character_maximum_length IS DISTINCT FROM expected.length
        )
        OR (SELECT count(*) FROM pg_constraint
            WHERE conrelid = 'public.owner_account'::regclass AND convalidated
            AND conname IN ('PK_OwnerAccount', 'CK_OwnerAccount_Slot', 'CK_OwnerAccount_LocalCredentials')) <> 3
        OR (SELECT count(*) FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid
            WHERE i.indrelid = 'public.owner_account'::regclass AND i.indisunique AND i.indisvalid
                AND i.indpred IS NULL AND i.indexprs IS NULL
                AND c.relname IN ('IX_OwnerAccount_Slot', 'IX_OwnerAccount_NormalizedEmail')) <> 2
    THEN
        RAISE EXCEPTION 'Owner account storage has an unsupported or incomplete schema. No existing data was changed.';
    END IF;
END
$upgrade$;

COMMIT;
