-- v15.0
CREATE TABLE IF NOT EXISTS "Widget" (
    "Id" UUID NOT NULL,
    "Title" VARCHAR(100) NOT NULL,
    "WidgetType" VARCHAR(50) NOT NULL,
    "ContentType" VARCHAR(25) NOT NULL,
    "ContentCode" VARCHAR(2000) NULL,
    "DisplayOrder" INTEGER NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL,
    "CreatedTimeUtc" TIMESTAMP NOT NULL,
    PRIMARY KEY ("Id")
);

-- v15.3
ALTER TABLE "Mention" DROP COLUMN IF EXISTS "Worker";

-- v15.4
ALTER TABLE "Post" DROP COLUMN IF EXISTS "HeroImageUrl";

-- v15.6
CREATE TABLE IF NOT EXISTS "ActivityLog" (
    "Id" BIGSERIAL NOT NULL,
    "EventId" INTEGER NOT NULL,
    "EventTimeUtc" TIMESTAMP NULL,
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
