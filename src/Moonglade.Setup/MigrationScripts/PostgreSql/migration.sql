-- v16.7
-- Refuse to discard existing replies; the operator must reconcile orphans first.
DO $migration$
BEGIN
    IF EXISTS (SELECT 1 FROM "CommentReply" r LEFT JOIN "Comment" c ON c."Id" = r."CommentId"
               WHERE r."CommentId" IS NULL OR c."Id" IS NULL) THEN
        RAISE EXCEPTION 'CommentReply contains orphan replies; reconcile them before migration.';
    END IF;
END
$migration$;

ALTER TABLE "CommentReply" DROP CONSTRAINT IF EXISTS "FK_CommentReply_Comment";
ALTER TABLE "CommentReply" ALTER COLUMN "CommentId" SET NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_CommentReply_CommentId" ON "CommentReply" ("CommentId");
ALTER TABLE "CommentReply" ADD CONSTRAINT "FK_CommentReply_Comment"
    FOREIGN KEY ("CommentId") REFERENCES "Comment" ("Id") ON DELETE CASCADE;

UPDATE "BlogConfiguration"
SET "CfgValue" = jsonb_build_object(
        'versionString', '16.8.1',
        'installTimeUtc', to_char(CURRENT_TIMESTAMP AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'))::text,
    "LastModifiedTimeUtc" = CURRENT_TIMESTAMP
WHERE "CfgKey" = 'SystemManifestSettings';
