-- PostgreSQL: upgrade the GUID-based Category schema to integer identity IDs.
-- Target: the release introducing category integer IDs (100, increment 1).
-- Prerequisites: public."Category" and public."PostCategory" with their original
-- primary keys, a Category foreign key, and a single-column CategoryId index.
-- Stop all application instances and back up the database before execution.
-- Run this standalone script before starting the upgraded application.
-- Category content and article GUIDs are preserved; old category GUIDs are not.

BEGIN;

DO $migration$
DECLARE
    category_table regclass := to_regclass('public."Category"');
    post_category_table regclass := to_regclass('public."PostCategory"');
    id_column smallint;
    category_id_column smallint;
    post_id_column smallint;
    id_type oid;
    category_id_type oid;
    category_pk text;
    category_pk_index oid;
    post_category_pk text;
    post_category_pk_index oid;
    category_fk text;
    category_fk_definition text;
    category_index oid;
    category_index_definition text;
    identity_sequence regclass;
    category_not_null text;
    post_category_not_null text;
    replacement_not_null text;
BEGIN
    IF category_table IS NULL OR post_category_table IS NULL THEN
        RAISE EXCEPTION 'Category migration requires public."Category" and public."PostCategory".';
    END IF;

    LOCK TABLE public."Category", public."PostCategory" IN ACCESS EXCLUSIVE MODE;

    SELECT attnum, atttypid INTO id_column, id_type
    FROM pg_attribute WHERE attrelid = category_table AND attname = 'Id' AND NOT attisdropped;
    SELECT attnum, atttypid INTO category_id_column, category_id_type
    FROM pg_attribute WHERE attrelid = post_category_table AND attname = 'CategoryId' AND NOT attisdropped;
    SELECT attnum INTO post_id_column
    FROM pg_attribute WHERE attrelid = post_category_table AND attname = 'PostId' AND NOT attisdropped;
    IF post_id_column IS NULL OR NOT EXISTS (
        SELECT 1 FROM pg_attribute WHERE attrelid = post_category_table AND attnum = post_id_column
            AND atttypid = 'uuid'::regtype) THEN
        RAISE EXCEPTION 'Category migration requires UUID article IDs in public."PostCategory"."PostId".';
    END IF;

    IF id_type = 'integer'::regtype AND category_id_type = 'integer'::regtype THEN
        identity_sequence := pg_get_serial_sequence('public."Category"', 'Id')::regclass;
        IF NOT EXISTS (
            SELECT 1 FROM pg_attribute WHERE attrelid = category_table AND attnum = id_column
                AND attidentity IN ('a', 'd') AND attnotnull)
            OR NOT EXISTS (
                SELECT 1 FROM pg_attribute WHERE attrelid = post_category_table
                    AND attnum = category_id_column AND attnotnull)
            OR NOT EXISTS (
                SELECT 1 FROM pg_sequence WHERE seqrelid = identity_sequence
                    AND seqstart = 100 AND seqincrement = 1) THEN
            RAISE EXCEPTION 'Integer category columns exist but do not match the required identity schema.';
        END IF;
        -- Do not reset the sequence or modify later administrator edits.
        RETURN;
    END IF;

    IF id_type IS NULL OR category_id_type IS NULL OR post_id_column IS NULL
        OR id_type <> 'uuid'::regtype OR category_id_type <> 'uuid'::regtype THEN
        RAISE EXCEPTION 'Category migration requires UUID columns, or an already upgraded integer identity schema.';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_attribute
        WHERE (attrelid = category_table AND attname = 'IdInt'
            OR attrelid = post_category_table AND attname = 'CategoryIdInt') AND NOT attisdropped) THEN
        RAISE EXCEPTION 'Temporary migration columns already exist. Restore or inspect the schema before retrying.';
    END IF;

    SELECT conname, conindid INTO category_pk, category_pk_index
    FROM pg_constraint WHERE conrelid = category_table AND contype = 'p'
        AND conkey = ARRAY[id_column];
    SELECT conname, conindid INTO post_category_pk, post_category_pk_index
    FROM pg_constraint WHERE conrelid = post_category_table AND contype = 'p'
        AND conkey = ARRAY[post_id_column, category_id_column];
    IF category_pk IS NULL OR post_category_pk IS NULL THEN
        RAISE EXCEPTION 'Unexpected category primary keys. Adapt the migration to this schema before execution.';
    END IF;

    SELECT conname, pg_get_constraintdef(oid) INTO category_fk, category_fk_definition
    FROM pg_constraint WHERE conrelid = post_category_table AND confrelid = category_table
        AND contype = 'f' AND conkey = ARRAY[category_id_column] AND confkey = ARRAY[id_column];
    IF category_fk IS NULL
        OR (SELECT count(*) FROM pg_constraint WHERE confrelid = category_table AND contype = 'f') <> 1 THEN
        RAISE EXCEPTION 'Unexpected category foreign keys. Adapt the migration to this schema before execution.';
    END IF;

    SELECT indexrelid, pg_get_indexdef(indexrelid) INTO category_index, category_index_definition
    FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid
    JOIN pg_am a ON a.oid = c.relam
    WHERE i.indrelid = post_category_table AND i.indnatts = 1 AND i.indkey[0] = category_id_column
        AND NOT i.indisunique AND i.indpred IS NULL AND a.amname = 'btree';
    IF category_index IS NULL
        OR EXISTS (
            SELECT 1 FROM pg_depend d JOIN pg_index i ON i.indexrelid = d.objid
            WHERE d.classid = 'pg_class'::regclass AND d.refclassid = 'pg_class'::regclass
                AND (d.refobjid = category_table AND d.refobjsubid = id_column
                    AND i.indexrelid <> category_pk_index
                    OR d.refobjid = post_category_table AND d.refobjsubid = category_id_column
                        AND i.indexrelid NOT IN (post_category_pk_index, category_index)))
        OR EXISTS (
            SELECT 1 FROM pg_constraint WHERE contype = 'c'
                AND (conrelid = category_table OR conrelid = post_category_table)) THEN
        RAISE EXCEPTION 'Unexpected category indexes or check constraints. Adapt the migration before execution.';
    END IF;

    -- PostgreSQL 18 represents NOT NULL as named constraints.
    SELECT conname INTO category_not_null
    FROM pg_constraint WHERE conrelid = category_table AND contype = 'n'
        AND conkey = ARRAY[id_column];
    SELECT conname INTO post_category_not_null
    FROM pg_constraint WHERE conrelid = post_category_table AND contype = 'n'
        AND conkey = ARRAY[category_id_column];

    ALTER TABLE public."Category" ADD COLUMN "IdInt" integer
        GENERATED BY DEFAULT AS IDENTITY (START WITH 100 INCREMENT BY 1);
    ALTER TABLE public."PostCategory" ADD COLUMN "CategoryIdInt" integer;

    UPDATE public."PostCategory" pc SET "CategoryIdInt" = c."IdInt"
    FROM public."Category" c WHERE c."Id" = pc."CategoryId";
    IF EXISTS (SELECT 1 FROM public."PostCategory" WHERE "CategoryIdInt" IS NULL) THEN
        RAISE EXCEPTION 'Unmapped article category. Repair orphaned associations before migration.';
    END IF;

    EXECUTE format('ALTER TABLE public."PostCategory" DROP CONSTRAINT %I', category_fk);
    EXECUTE format('ALTER TABLE public."PostCategory" DROP CONSTRAINT %I', post_category_pk);
    EXECUTE format('ALTER TABLE public."Category" DROP CONSTRAINT %I', category_pk);
    EXECUTE format('DROP INDEX %s', category_index::regclass);

    ALTER TABLE public."PostCategory" DROP COLUMN "CategoryId";
    ALTER TABLE public."Category" DROP COLUMN "Id";
    ALTER TABLE public."PostCategory" ALTER COLUMN "CategoryIdInt" SET NOT NULL;
    ALTER TABLE public."Category" RENAME COLUMN "IdInt" TO "Id";
    ALTER TABLE public."PostCategory" RENAME COLUMN "CategoryIdInt" TO "CategoryId";

    EXECUTE format('ALTER TABLE public."Category" ADD CONSTRAINT %I PRIMARY KEY ("Id")', category_pk);
    EXECUTE format('ALTER TABLE public."PostCategory" ADD CONSTRAINT %I PRIMARY KEY ("PostId", "CategoryId")', post_category_pk);
    EXECUTE category_index_definition;
    EXECUTE format('ALTER TABLE public."PostCategory" ADD CONSTRAINT %I %s', category_fk, category_fk_definition);

    IF category_not_null IS NOT NULL THEN
        SELECT c.conname INTO replacement_not_null
        FROM pg_constraint c JOIN pg_attribute a ON a.attrelid = c.conrelid AND c.conkey = ARRAY[a.attnum]
        WHERE c.conrelid = category_table AND c.contype = 'n' AND a.attname = 'Id';
        IF replacement_not_null IS NULL THEN
            RAISE EXCEPTION 'Missing NOT NULL constraint on the migrated Category.Id.';
        END IF;
        IF replacement_not_null <> category_not_null THEN
            EXECUTE format('ALTER TABLE public."Category" RENAME CONSTRAINT %I TO %I',
                replacement_not_null, category_not_null);
        END IF;
    END IF;
    IF post_category_not_null IS NOT NULL THEN
        SELECT c.conname INTO replacement_not_null
        FROM pg_constraint c JOIN pg_attribute a ON a.attrelid = c.conrelid AND c.conkey = ARRAY[a.attnum]
        WHERE c.conrelid = post_category_table AND c.contype = 'n' AND a.attname = 'CategoryId';
        IF replacement_not_null IS NULL THEN
            RAISE EXCEPTION 'Missing NOT NULL constraint on the migrated PostCategory.CategoryId.';
        END IF;
        IF replacement_not_null <> post_category_not_null THEN
            EXECUTE format('ALTER TABLE public."PostCategory" RENAME CONSTRAINT %I TO %I',
                replacement_not_null, post_category_not_null);
        END IF;
    END IF;
END;
$migration$;

COMMIT;
