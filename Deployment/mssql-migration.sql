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

-- v15.0
CREATE TABLE [Widget](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[WidgetType] [nvarchar](50) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsEnabled] [bit] NOT NULL,
	[CreatedTimeUtc] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [WidgetLinkItem](
	[Id] [uniqueidentifier] NOT NULL,
	[WidgetId] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Url] [nvarchar](500) NOT NULL,
	[IconName] [nvarchar](50) NULL,
	[OpenInNewWindow] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsEnabled] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [WidgetLinkItem]  WITH CHECK ADD  CONSTRAINT [FK_WidgetLinkItem_Widget] FOREIGN KEY([WidgetId])
REFERENCES [Widget] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [WidgetLinkItem] CHECK CONSTRAINT [FK_WidgetLinkItem_Widget]
GO
