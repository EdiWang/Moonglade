-- v15.6
CREATE TABLE IF NOT EXISTS "ActivityLog" (
    "Id" BIGSERIAL NOT NULL,
    "EventId" INTEGER NOT NULL,
    "EventTimeUtc" TIMESTAMP WITH TIME ZONE NULL,
    "ActorId" VARCHAR(100) NULL,
    "Operation" VARCHAR(100) NULL,
    "TargetName" VARCHAR(200) NULL,
    "MetaData" TEXT NULL,
    "IpAddress" VARCHAR(50) NULL,
    "UserAgent" VARCHAR(512) NULL,
    PRIMARY KEY ("Id")
);

-- v15.7
-- Rename `CustomPage` table to `BlogPage`
ALTER TABLE IF EXISTS "CustomPage" RENAME TO "BlogPage";

-- v15.12
-- Add ContentType column to Post table
ALTER TABLE "Post" ADD COLUMN IF NOT EXISTS "ContentType" VARCHAR(16) NOT NULL DEFAULT '';

-- v15.16
-- Add IsDeleted column to BlogPage table
ALTER TABLE "BlogPage" ADD COLUMN IF NOT EXISTS "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE;

-- Add ContainsAiAssistedContent column to Post table
ALTER TABLE "Post" ADD COLUMN IF NOT EXISTS "ContainsAiAssistedContent" BOOLEAN NOT NULL DEFAULT FALSE;

-- v15.18
-- Add daily post view aggregation table
CREATE TABLE IF NOT EXISTS "PostViewDaily" (
    "PostId" UUID NOT NULL,
    "ViewDateUtc" DATE NOT NULL,
    "ViewCount" INTEGER NOT NULL,
    PRIMARY KEY ("PostId", "ViewDateUtc")
);

CREATE INDEX IF NOT EXISTS "IX_PostViewDaily_ViewDateUtc" ON "PostViewDaily" ("ViewDateUtc");

-- v16.2
-- Add durable email outbox table
CREATE TABLE IF NOT EXISTS "EmailOutboxMessage" (
    "Id" UUID NOT NULL,
    "MessageType" VARCHAR(100) NOT NULL,
    "DistributionList" VARCHAR(4000) NOT NULL,
    "MessageBody" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "AttemptCount" INTEGER NOT NULL,
    "CreatedTimeUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LastAttemptTimeUtc" TIMESTAMP WITH TIME ZONE NULL,
    "NotBeforeUtc" TIMESTAMP WITH TIME ZONE NULL,
    "LockedUntilUtc" TIMESTAMP WITH TIME ZONE NULL,
    "LockedBy" VARCHAR(128) NULL,
    "SentTimeUtc" TIMESTAMP WITH TIME ZONE NULL,
    "LastError" VARCHAR(2000) NULL,
    "ConcurrencyToken" UUID NOT NULL,
    PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_EmailOutboxMessage_Dequeue"
ON "EmailOutboxMessage" ("Status", "NotBeforeUtc", "LockedUntilUtc", "CreatedTimeUtc");

-- v16.4
-- Add site verification files table
CREATE TABLE IF NOT EXISTS "SiteVerificationFile" (
    "Id" UUID NOT NULL,
    "FileName" VARCHAR(128) NOT NULL,
    "NormalizedFileName" VARCHAR(128) NOT NULL,
    "Content" VARCHAR(65536) NOT NULL,
    "ContentType" VARCHAR(64) NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL,
    "CreatedTimeUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LastModifiedTimeUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_SiteVerificationFile_NormalizedFileName"
ON "SiteVerificationFile" ("NormalizedFileName");

-- v16.4
-- Existing timestamp-without-time-zone values represent UTC wall-clock values.
-- AT TIME ZONE 'UTC' attaches that contract explicitly and is independent of the
-- PostgreSQL session time zone.
DROP INDEX IF EXISTS "IX_EmailOutboxMessage_Dequeue";

DO $migration$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'ActivityLog' AND column_name = 'EventTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "ActivityLog" ALTER COLUMN "EventTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "EventTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'BlogAsset' AND column_name = 'LastModifiedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "BlogAsset" ALTER COLUMN "LastModifiedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LastModifiedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'BlogConfiguration' AND column_name = 'LastModifiedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "BlogConfiguration" ALTER COLUMN "LastModifiedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LastModifiedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'BlogPage' AND column_name = 'CreateTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "BlogPage" ALTER COLUMN "CreateTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreateTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'BlogPage' AND column_name = 'UpdateTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "BlogPage" ALTER COLUMN "UpdateTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "UpdateTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Comment' AND column_name = 'CreateTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Comment" ALTER COLUMN "CreateTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreateTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'CommentReply' AND column_name = 'CreateTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "CommentReply" ALTER COLUMN "CreateTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreateTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EmailOutboxMessage' AND column_name = 'CreatedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "EmailOutboxMessage" ALTER COLUMN "CreatedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreatedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EmailOutboxMessage' AND column_name = 'LastAttemptTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "EmailOutboxMessage" ALTER COLUMN "LastAttemptTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LastAttemptTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EmailOutboxMessage' AND column_name = 'NotBeforeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "EmailOutboxMessage" ALTER COLUMN "NotBeforeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "NotBeforeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EmailOutboxMessage' AND column_name = 'LockedUntilUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "EmailOutboxMessage" ALTER COLUMN "LockedUntilUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LockedUntilUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EmailOutboxMessage' AND column_name = 'SentTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "EmailOutboxMessage" ALTER COLUMN "SentTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "SentTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Mention' AND column_name = 'PingTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Mention" ALTER COLUMN "PingTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "PingTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Post' AND column_name = 'CreateTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Post" ALTER COLUMN "CreateTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreateTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Post' AND column_name = 'PubDateUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Post" ALTER COLUMN "PubDateUtc" TYPE TIMESTAMP WITH TIME ZONE USING "PubDateUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Post' AND column_name = 'LastModifiedUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Post" ALTER COLUMN "LastModifiedUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LastModifiedUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Post' AND column_name = 'ScheduledPublishTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Post" ALTER COLUMN "ScheduledPublishTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "ScheduledPublishTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'PostView' AND column_name = 'BeginTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "PostView" ALTER COLUMN "BeginTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "BeginTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'StyleSheet' AND column_name = 'LastModifiedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "StyleSheet" ALTER COLUMN "LastModifiedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LastModifiedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'Widget' AND column_name = 'CreatedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "Widget" ALTER COLUMN "CreatedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreatedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'SiteVerificationFile' AND column_name = 'CreatedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "SiteVerificationFile" ALTER COLUMN "CreatedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "CreatedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'SiteVerificationFile' AND column_name = 'LastModifiedTimeUtc' AND data_type = 'timestamp without time zone') THEN
        ALTER TABLE "SiteVerificationFile" ALTER COLUMN "LastModifiedTimeUtc" TYPE TIMESTAMP WITH TIME ZONE USING "LastModifiedTimeUtc" AT TIME ZONE 'UTC';
    END IF;
END
$migration$;

CREATE INDEX IF NOT EXISTS "IX_EmailOutboxMessage_Dequeue"
ON "EmailOutboxMessage" ("Status", "NotBeforeUtc", "LockedUntilUtc", "CreatedTimeUtc");

DO $migration$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'PostViewDaily' AND column_name = 'ViewDateUtc' AND data_type <> 'date') THEN
        DROP INDEX IF EXISTS "IX_PostViewDaily_ViewDateUtc";
        ALTER TABLE "PostViewDaily" DROP CONSTRAINT IF EXISTS "PK_PostViewDaily";
        ALTER TABLE "PostViewDaily" ALTER COLUMN "ViewDateUtc" TYPE DATE USING "ViewDateUtc"::date;
    END IF;
END
$migration$;

DO $migration$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = '"PostViewDaily"'::regclass AND conname = 'PK_PostViewDaily') THEN
        ALTER TABLE "PostViewDaily" ADD CONSTRAINT "PK_PostViewDaily" PRIMARY KEY ("PostId", "ViewDateUtc");
    END IF;
END
$migration$;

CREATE INDEX IF NOT EXISTS "IX_PostViewDaily_ViewDateUtc" ON "PostViewDaily" ("ViewDateUtc");

DROP TABLE IF EXISTS "LoginHistory";

UPDATE "BlogConfiguration"
SET "CfgValue" = jsonb_build_object(
        'versionString', '16.4.0',
        'installTimeUtc', to_char(CURRENT_TIMESTAMP AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"'))::text,
    "LastModifiedTimeUtc" = CURRENT_TIMESTAMP
WHERE "CfgKey" = 'SystemManifestSettings';
