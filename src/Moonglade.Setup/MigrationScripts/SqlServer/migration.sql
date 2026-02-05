-- v15.0
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Widget]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Widget](
        [Id] [uniqueidentifier] NOT NULL,
        [Title] [nvarchar](100) NOT NULL,
        [WidgetType] [nvarchar](50) NOT NULL,
        [ContentType] [nvarchar](25) NOT NULL,
        [ContentCode] [nvarchar](2000) NULL,
        [DisplayOrder] [int] NOT NULL,
        [IsEnabled] [bit] NOT NULL,
        [CreatedTimeUtc] [datetime] NOT NULL,
    PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

-- v15.3
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Mention]') AND name = 'Worker')
BEGIN
    ALTER TABLE [dbo].[Mention] DROP COLUMN [Worker];
END
GO

-- v15.4
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'HeroImageUrl')
BEGIN
    ALTER TABLE [dbo].[Post] DROP COLUMN [HeroImageUrl];
END