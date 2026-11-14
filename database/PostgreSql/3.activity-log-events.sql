-- PostgreSQL: replace legacy activity history with stable activity event keys.
-- Target: the release introducing stable activity event keys.
-- Prerequisites: category integer IDs, then 2.snake-case-names.sql if needed;
-- public.activity_log must have the complete standard legacy or new shape.
-- Stop all application instances/background workers, back up, and rehearse on
-- a restored copy. Run this complete script standalone as the table owner.
-- Only the recognized legacy cutover deletes activity history. Other tables,
-- the existing table/primary key/identity sequence, and new history are retained.

BEGIN;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '5min';

DO $migration$
DECLARE
    activity_table regclass := to_regclass('public.activity_log');
    id_column smallint;
    primary_key oid;
    primary_key_index oid;
    identity_sequence regclass;
    legacy_shape boolean;
    new_shape boolean;
BEGIN
    IF activity_table IS NULL THEN
        RAISE EXCEPTION 'Activity upgrade requires public.activity_log; complete prerequisite upgrades first. No history was deleted.';
    END IF;

    LOCK TABLE public.activity_log IN ACCESS EXCLUSIVE MODE;

    IF to_regclass('public.activity_log') IS DISTINCT FROM activity_table THEN
        RAISE EXCEPTION 'Activity table identity changed while acquiring the lock. No history was deleted.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_class
        WHERE oid = activity_table AND relkind = 'r' AND relpersistence = 'p'
            AND NOT relispartition AND NOT relrowsecurity AND NOT relforcerowsecurity)
        OR EXISTS (
            SELECT 1 FROM pg_inherits
            WHERE inhrelid = activity_table OR inhparent = activity_table) THEN
        RAISE EXCEPTION 'Unexpected activity table inheritance, partitioning, persistence, or row security. No history was deleted.';
    END IF;

    SELECT count(*) = 9 AND NOT EXISTS (
        SELECT 1
        FROM (VALUES
            ('id', 'bigint'::regtype, -1, true),
            ('event_id', 'integer'::regtype, -1, true),
            ('event_time_utc', 'timestamp with time zone'::regtype, -1, false),
            ('actor_id', 'character varying'::regtype, 104, false),
            ('operation', 'character varying'::regtype, 104, false),
            ('target_name', 'character varying'::regtype, 204, false),
            ('meta_data', 'text'::regtype, -1, false),
            ('ip_address', 'character varying'::regtype, 54, false),
            ('user_agent', 'character varying'::regtype, 516, false)
        ) AS expected(column_name, column_type, type_modifier, required)
        LEFT JOIN pg_attribute a ON a.attrelid = activity_table
            AND a.attname = expected.column_name AND a.attnum > 0 AND NOT a.attisdropped
        WHERE a.attnum IS NULL OR a.atttypid <> expected.column_type
            OR a.atttypmod <> expected.type_modifier OR a.attnotnull <> expected.required
    ) INTO legacy_shape
    FROM pg_attribute
    WHERE attrelid = activity_table AND attnum > 0 AND NOT attisdropped;

    SELECT count(*) = 10 AND NOT EXISTS (
        SELECT 1
        FROM (VALUES
            ('id', 'bigint'::regtype, -1, true),
            ('event_key', 'character varying'::regtype, 104, true),
            ('event_time_utc', 'timestamp with time zone'::regtype, -1, true),
            ('actor_kind', 'integer'::regtype, -1, true),
            ('actor_display_name', 'character varying'::regtype, 104, false),
            ('target_id', 'character varying'::regtype, 104, false),
            ('target_display_name', 'character varying'::regtype, 204, false),
            ('ip_address', 'character varying'::regtype, 54, false),
            ('user_agent', 'character varying'::regtype, 516, false),
            ('details', 'text'::regtype, -1, false)
        ) AS expected(column_name, column_type, type_modifier, required)
        LEFT JOIN pg_attribute a ON a.attrelid = activity_table
            AND a.attname = expected.column_name AND a.attnum > 0 AND NOT a.attisdropped
        WHERE a.attnum IS NULL OR a.atttypid <> expected.column_type
            OR a.atttypmod <> expected.type_modifier OR a.attnotnull <> expected.required
    ) INTO new_shape
    FROM pg_attribute
    WHERE attrelid = activity_table AND attnum > 0 AND NOT attisdropped;

    IF NOT legacy_shape AND NOT new_shape THEN
        RAISE EXCEPTION 'Unrecognized activity column names, types, lengths, or nullability (expected all 9 legacy or all 10 new columns). No history was deleted.';
    END IF;

    SELECT attnum INTO id_column
    FROM pg_attribute WHERE attrelid = activity_table AND attname = 'id' AND NOT attisdropped;
    identity_sequence := pg_get_serial_sequence('public.activity_log', 'id')::regclass;
    IF NOT EXISTS (
        SELECT 1 FROM pg_attribute WHERE attrelid = activity_table AND attnum = id_column
            AND attidentity IN ('a', 'd') AND attnotnull)
        OR NOT EXISTS (
            SELECT 1 FROM pg_sequence
            WHERE seqrelid = identity_sequence AND seqtypid = 'bigint'::regtype)
        OR EXISTS (
            SELECT 1 FROM pg_attribute
            WHERE attrelid = activity_table AND attnum > 0 AND NOT attisdropped
                AND (attgenerated <> '' OR atthasdef
                    OR (attnum <> id_column AND attidentity <> ''))) THEN
        RAISE EXCEPTION 'Unexpected activity identity, generated column, or default. No history was deleted.';
    END IF;

    SELECT oid, conindid INTO primary_key, primary_key_index
    FROM pg_constraint
    WHERE conrelid = activity_table AND contype = 'p' AND conkey = ARRAY[id_column]
        AND convalidated AND NOT condeferrable;
    IF primary_key IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM pg_index i
            JOIN pg_class c ON c.oid = i.indexrelid
            JOIN pg_am a ON a.oid = c.relam
            WHERE i.indexrelid = primary_key_index AND i.indisprimary AND i.indisunique
                AND i.indisvalid AND i.indisready AND i.indnatts = 1
                AND i.indkey[0] = id_column AND i.indpred IS NULL
                AND i.indexprs IS NULL AND a.amname = 'btree')
        OR EXISTS (
            SELECT 1 FROM pg_index
            WHERE indrelid = activity_table AND indexrelid <> primary_key_index)
        OR EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE (contype = 'f' AND confrelid = activity_table)
                OR (conrelid = activity_table AND oid <> primary_key
                    AND (contype <> 'n' OR cardinality(conkey) <> 1
                        OR NOT convalidated OR NOT EXISTS (
                            SELECT 1 FROM pg_attribute
                            WHERE attrelid = activity_table AND attnum = conkey[1]
                                AND attnotnull AND NOT attisdropped)))) THEN
        RAISE EXCEPTION 'Unexpected activity primary key, foreign key, index, or constraint. No history was deleted.';
    END IF;

    -- NOT NULL constraints are catalog objects in PostgreSQL 18. Allow those,
    -- the row/TOAST types, retained primary key, and owned identity sequence only.
    IF EXISTS (
        SELECT 1 FROM pg_depend d
        WHERE d.refclassid = 'pg_class'::regclass AND d.refobjid = activity_table
            AND NOT (
                (d.classid = 'pg_type'::regclass AND d.deptype = 'i'
                    AND d.refobjsubid = 0 AND d.objid = (
                        SELECT reltype FROM pg_class WHERE oid = activity_table))
                OR (d.classid = 'pg_class'::regclass AND (
                    d.objid = primary_key_index
                    OR (d.objid = identity_sequence AND d.refobjsubid = id_column
                        AND d.deptype = 'i')
                    OR (d.objid = (SELECT reltoastrelid FROM pg_class WHERE oid = activity_table)
                        AND d.refobjsubid = 0 AND d.deptype = 'i')))
                OR (d.classid = 'pg_constraint'::regclass AND (
                    d.objid = primary_key OR EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE oid = d.objid AND conrelid = activity_table AND contype = 'n')))
            ))
        OR EXISTS (SELECT 1 FROM pg_trigger WHERE tgrelid = activity_table)
        OR EXISTS (SELECT 1 FROM pg_rewrite WHERE ev_class = activity_table)
        OR EXISTS (SELECT 1 FROM pg_event_trigger WHERE evtenabled <> 'D') THEN
        RAISE EXCEPTION 'Unexpected activity dependency, trigger, rule, policy, or database event trigger. Have a DBA inspect the schema. No history was deleted.';
    END IF;

    IF new_shape THEN
        -- Do not delete rows, consume sequence values, or reset the generator.
        RETURN;
    END IF;

    DELETE FROM public.activity_log;
    ALTER TABLE public.activity_log
        DROP COLUMN event_id,
        DROP COLUMN actor_id,
        DROP COLUMN operation,
        DROP COLUMN target_name,
        DROP COLUMN meta_data;
    ALTER TABLE public.activity_log ALTER COLUMN event_time_utc SET NOT NULL;
    ALTER TABLE public.activity_log
        ADD COLUMN event_key character varying(100) NOT NULL,
        ADD COLUMN actor_kind integer NOT NULL,
        ADD COLUMN actor_display_name character varying(100),
        ADD COLUMN target_id character varying(100),
        ADD COLUMN target_display_name character varying(200),
        ADD COLUMN details text;
END
$migration$;

COMMIT;
