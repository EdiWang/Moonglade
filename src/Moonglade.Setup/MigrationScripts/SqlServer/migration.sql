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
GO

-- v15.6
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ActivityLog]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[ActivityLog](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[EventId] [int] NOT NULL,
	[EventTimeUtc] [datetime] NULL,
	[ActorId] [nvarchar](100) NULL,
	[Operation] [nvarchar](100) NULL,
	[TargetName] [nvarchar](200) NULL,
	[MetaData] [nvarchar](max) NULL,
	[IpAddress] [nvarchar](50) NULL,
	[UserAgent] [nvarchar](512) NULL,
 CONSTRAINT [PK_ActivityLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- v15.7
-- Rename `CustomPage` table to `BlogPage`
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CustomPage]') AND type in (N'U'))
BEGIN
    EXEC sp_rename 'CustomPage', 'BlogPage';
END
GO