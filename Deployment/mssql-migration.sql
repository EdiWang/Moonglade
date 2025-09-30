-- v14.19
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Post'
      AND COLUMN_NAME = 'Revision'
)
BEGIN
    ALTER TABLE [dbo].[Post] DROP COLUMN Revision;
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects 
               WHERE object_id = OBJECT_ID(N'[dbo].[PostView]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[PostView](
		[PostId] [uniqueidentifier] NOT NULL,
		[RequestCount] [int] NOT NULL,
		[ViewCount] [int] NOT NULL,
        [BeginTimeUtc] [datetime] NOT NULL,
	 CONSTRAINT [PK_PostView] PRIMARY KEY CLUSTERED 
	(
		[PostId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY]
END
GO

-- v14.22
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Post') 
      AND name = 'PostStatus'
)
BEGIN
    ALTER TABLE dbo.Post ADD PostStatus VARCHAR(16) NULL;
END
GO

IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Post') 
      AND name = 'IsPublished'
)
BEGIN
    EXEC('
        UPDATE dbo.Post
        SET PostStatus = ''published''
        WHERE IsPublished = 1;

        UPDATE dbo.Post
        SET PostStatus = ''draft''
        WHERE IsPublished = 0;

        ALTER TABLE dbo.Post DROP COLUMN IsPublished;
    ')
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Post') 
      AND name = 'ScheduledPublishTimeUtc'
)
BEGIN
    ALTER TABLE dbo.Post ADD ScheduledPublishTimeUtc DATETIME NULL;
END
GO

-- v14.28
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Post') 
      AND name = 'Keywords'
)
BEGIN
    ALTER TABLE dbo.Post ADD Keywords NVARCHAR(256) NULL;
END
GO
