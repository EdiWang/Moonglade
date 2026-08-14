-- Supported upgrade baseline: Moonglade v16.3.0
CREATE TABLE "ActivityLog" (
    "Id" BIGSERIAL NOT NULL PRIMARY KEY,
    "EventId" INTEGER NOT NULL,
    "EventTimeUtc" TIMESTAMP NULL,
    "ActorId" VARCHAR(100) NULL,
    "Operation" VARCHAR(100) NULL,
    "TargetName" VARCHAR(200) NULL,
    "MetaData" TEXT NULL,
    "IpAddress" VARCHAR(50) NULL,
    "UserAgent" VARCHAR(512) NULL
);

CREATE TABLE "BlogAsset" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "LastModifiedTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "BlogConfiguration" (
    "CfgKey" VARCHAR(64) NOT NULL PRIMARY KEY,
    "CfgValue" TEXT NOT NULL,
    "LastModifiedTimeUtc" TIMESTAMP NULL
);

CREATE TABLE "BlogPage" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "CreateTimeUtc" TIMESTAMP NOT NULL,
    "UpdateTimeUtc" TIMESTAMP NULL,
    "IsDeleted" BOOLEAN NOT NULL
);

CREATE TABLE "Comment" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "CreateTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "CommentReply" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "CreateTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "EmailOutboxMessage" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "MessageType" VARCHAR(100) NOT NULL,
    "DistributionList" VARCHAR(4000) NOT NULL,
    "MessageBody" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "AttemptCount" INTEGER NOT NULL,
    "CreatedTimeUtc" TIMESTAMP NOT NULL,
    "LastAttemptTimeUtc" TIMESTAMP NULL,
    "NotBeforeUtc" TIMESTAMP NULL,
    "LockedUntilUtc" TIMESTAMP NULL,
    "LockedBy" VARCHAR(128) NULL,
    "SentTimeUtc" TIMESTAMP NULL,
    "LastError" VARCHAR(2000) NULL,
    "ConcurrencyToken" UUID NOT NULL
);

CREATE INDEX "IX_EmailOutboxMessage_Dequeue"
ON "EmailOutboxMessage" ("Status", "NotBeforeUtc", "LockedUntilUtc", "CreatedTimeUtc");

CREATE TABLE "LoginHistory" (
    "Id" BIGSERIAL NOT NULL PRIMARY KEY,
    "LoginTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "Mention" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "PingTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "Post" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "CreateTimeUtc" TIMESTAMP NOT NULL,
    "PubDateUtc" TIMESTAMP NULL,
    "LastModifiedUtc" TIMESTAMP NULL,
    "ScheduledPublishTimeUtc" TIMESTAMP NULL,
    "ContentType" VARCHAR(16) NOT NULL,
    "ContainsAiAssistedContent" BOOLEAN NOT NULL
);

CREATE TABLE "PostView" (
    "Id" BIGSERIAL NOT NULL PRIMARY KEY,
    "BeginTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "PostViewDaily" (
    "PostId" UUID NOT NULL,
    "ViewDateUtc" TIMESTAMP NOT NULL,
    "ViewCount" INTEGER NOT NULL,
    CONSTRAINT "PK_PostViewDaily" PRIMARY KEY ("PostId", "ViewDateUtc")
);

CREATE INDEX "IX_PostViewDaily_ViewDateUtc" ON "PostViewDaily" ("ViewDateUtc");

CREATE TABLE "StyleSheet" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "LastModifiedTimeUtc" TIMESTAMP NOT NULL
);

CREATE TABLE "Widget" (
    "Id" UUID NOT NULL PRIMARY KEY,
    "Title" VARCHAR(100) NOT NULL,
    "WidgetType" VARCHAR(50) NOT NULL,
    "ContentType" VARCHAR(25) NOT NULL,
    "ContentCode" VARCHAR(2000) NULL,
    "DisplayOrder" INTEGER NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL,
    "CreatedTimeUtc" TIMESTAMP NOT NULL
);

INSERT INTO "Post" (
    "Id", "CreateTimeUtc", "PubDateUtc", "LastModifiedUtc", "ScheduledPublishTimeUtc", "ContentType", "ContainsAiAssistedContent"
) VALUES (
    '11111111-1111-1111-1111-111111111111', TIMESTAMP '2010-01-02 03:04:05.123456',
    TIMESTAMP '2025-12-31 23:59:59.999999', NULL, NULL, 'HTML', FALSE
);

INSERT INTO "PostViewDaily" ("PostId", "ViewDateUtc", "ViewCount")
VALUES ('11111111-1111-1111-1111-111111111111', TIMESTAMP '2026-08-13 00:00:00', 7);

INSERT INTO "EmailOutboxMessage" (
    "Id", "MessageType", "DistributionList", "MessageBody", "Status", "AttemptCount", "CreatedTimeUtc",
    "LastAttemptTimeUtc", "NotBeforeUtc", "LockedUntilUtc", "LockedBy", "SentTimeUtc", "LastError", "ConcurrencyToken"
) VALUES (
    '22222222-2222-2222-2222-222222222222', 'Test', 'test@example.com', 'fixture', 0, 0,
    TIMESTAMP '2026-08-14 01:02:03.123456', NULL, TIMESTAMP '2026-08-14 01:05:00', NULL, NULL, NULL, NULL,
    '33333333-3333-3333-3333-333333333333'
);

INSERT INTO "BlogConfiguration" ("CfgKey", "CfgValue", "LastModifiedTimeUtc")
VALUES ('SystemManifestSettings', 'fixture', TIMESTAMP '2026-01-01 00:00:00');
