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
