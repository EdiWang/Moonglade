-- PostgreSQL: upgrade the owner OIDC schema from 16.10.0 to 16.11.0 after 10.owner-oidc.sql.
-- Run with all hosts stopped. Existing bindings, including disabled ones, remain usable.
BEGIN;

ALTER TABLE public.owner_oidc DROP COLUMN IF EXISTS enabled;

COMMIT;
