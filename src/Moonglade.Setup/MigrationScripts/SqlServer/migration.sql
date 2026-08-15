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
        [CreatedTimeUtc] [datetime2](7) NOT NULL,
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
	[EventTimeUtc] [datetime2](7) NULL,
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

-- v15.12
-- Add ContentType column to Post table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'ContentType')
BEGIN
    ALTER TABLE [dbo].[Post] ADD [ContentType] [nvarchar](16) NOT NULL
        CONSTRAINT [DF_Post_ContentType] DEFAULT '';
END
GO

-- v15.16
-- Add IsDeleted column to BlogPage table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BlogPage]') AND name = 'IsDeleted')
BEGIN
    ALTER TABLE [dbo].[BlogPage] ADD [IsDeleted] [bit] NOT NULL
        CONSTRAINT [DF_BlogPage_IsDeleted] DEFAULT 0;
END
GO

-- Add ContainsAiAssistedContent column to Post table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'ContainsAiAssistedContent')
BEGIN
    ALTER TABLE [dbo].[Post] ADD [ContainsAiAssistedContent] [bit] NOT NULL
        CONSTRAINT [DF_Post_ContainsAiAssistedContent] DEFAULT 0;
END
GO

-- v15.18
-- Add daily post view aggregation table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PostViewDaily]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PostViewDaily](
        [PostId] [uniqueidentifier] NOT NULL,
        [ViewDateUtc] [date] NOT NULL,
        [ViewCount] [int] NOT NULL,
        CONSTRAINT [PK_PostViewDaily] PRIMARY KEY CLUSTERED
        (
            [PostId] ASC,
            [ViewDateUtc] ASC
        )
    ) ON [PRIMARY]

    CREATE INDEX [IX_PostViewDaily_ViewDateUtc] ON [dbo].[PostViewDaily]([ViewDateUtc]);
END
GO

-- v16.2
-- Add durable email outbox table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmailOutboxMessage](
        [Id] [uniqueidentifier] NOT NULL,
        [MessageType] [nvarchar](100) NOT NULL,
        [DistributionList] [nvarchar](4000) NOT NULL,
        [MessageBody] [nvarchar](max) NOT NULL,
        [Status] [int] NOT NULL,
        [AttemptCount] [int] NOT NULL,
        [CreatedTimeUtc] [datetime2](7) NOT NULL,
        [LastAttemptTimeUtc] [datetime2](7) NULL,
        [NotBeforeUtc] [datetime2](7) NULL,
        [LockedUntilUtc] [datetime2](7) NULL,
        [LockedBy] [nvarchar](128) NULL,
        [SentTimeUtc] [datetime2](7) NULL,
        [LastError] [nvarchar](2000) NULL,
        [ConcurrencyToken] [uniqueidentifier] NOT NULL,
        CONSTRAINT [PK_EmailOutboxMessage] PRIMARY KEY CLUSTERED
        (
            [Id] ASC
        )
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = N'IX_EmailOutboxMessage_Dequeue')
BEGIN
    CREATE INDEX [IX_EmailOutboxMessage_Dequeue]
    ON [dbo].[EmailOutboxMessage]([Status], [NotBeforeUtc], [LockedUntilUtc], [CreatedTimeUtc]);
END
GO

-- v16.4
-- Add site verification files table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SiteVerificationFile]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SiteVerificationFile](
        [Id] [uniqueidentifier] NOT NULL,
        [FileName] [nvarchar](128) NOT NULL,
        [NormalizedFileName] [nvarchar](128) NOT NULL,
        [Content] [nvarchar](max) NOT NULL,
        [ContentType] [nvarchar](64) NOT NULL,
        [IsEnabled] [bit] NOT NULL,
        [CreatedTimeUtc] [datetime2](7) NOT NULL,
        [LastModifiedTimeUtc] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_SiteVerificationFile] PRIMARY KEY CLUSTERED
        (
            [Id] ASC
        )
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- v16.4
-- Normalize persisted UTC timestamps and daily UTC view keys.
-- SQL Server datetime values already represent UTC wall-clock values, so changing
-- their storage type preserves the value while increasing precision.
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = N'IX_EmailOutboxMessage_Dequeue')
BEGIN
    DROP INDEX [IX_EmailOutboxMessage_Dequeue] ON [dbo].[EmailOutboxMessage];
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ActivityLog]') AND name = 'EventTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[ActivityLog] ALTER COLUMN [EventTimeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BlogAsset]') AND name = 'LastModifiedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[BlogAsset] ALTER COLUMN [LastModifiedTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BlogConfiguration]') AND name = 'LastModifiedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[BlogConfiguration] ALTER COLUMN [LastModifiedTimeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BlogPage]') AND name = 'CreateTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[BlogPage] ALTER COLUMN [CreateTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BlogPage]') AND name = 'UpdateTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[BlogPage] ALTER COLUMN [UpdateTimeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Comment]') AND name = 'CreateTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Comment] ALTER COLUMN [CreateTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CommentReply]') AND name = 'CreateTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[CommentReply] ALTER COLUMN [CreateTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = 'CreatedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[EmailOutboxMessage] ALTER COLUMN [CreatedTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = 'LastAttemptTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[EmailOutboxMessage] ALTER COLUMN [LastAttemptTimeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = 'NotBeforeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[EmailOutboxMessage] ALTER COLUMN [NotBeforeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = 'LockedUntilUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[EmailOutboxMessage] ALTER COLUMN [LockedUntilUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = 'SentTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[EmailOutboxMessage] ALTER COLUMN [SentTimeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Mention]') AND name = 'PingTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Mention] ALTER COLUMN [PingTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'CreateTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Post] ALTER COLUMN [CreateTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'PubDateUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Post] ALTER COLUMN [PubDateUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'LastModifiedUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Post] ALTER COLUMN [LastModifiedUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Post]') AND name = 'ScheduledPublishTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Post] ALTER COLUMN [ScheduledPublishTimeUtc] [datetime2](7) NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostView]') AND name = 'BeginTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[PostView] ALTER COLUMN [BeginTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StyleSheet]') AND name = 'LastModifiedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[StyleSheet] ALTER COLUMN [LastModifiedTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Widget]') AND name = 'CreatedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[Widget] ALTER COLUMN [CreatedTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SiteVerificationFile]') AND name = 'CreatedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[SiteVerificationFile] ALTER COLUMN [CreatedTimeUtc] [datetime2](7) NOT NULL;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SiteVerificationFile]') AND name = 'LastModifiedTimeUtc' AND system_type_id <> TYPE_ID(N'datetime2'))
    ALTER TABLE [dbo].[SiteVerificationFile] ALTER COLUMN [LastModifiedTimeUtc] [datetime2](7) NOT NULL;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EmailOutboxMessage]') AND name = N'IX_EmailOutboxMessage_Dequeue')
BEGIN
    CREATE INDEX [IX_EmailOutboxMessage_Dequeue]
    ON [dbo].[EmailOutboxMessage]([Status], [NotBeforeUtc], [LockedUntilUtc], [CreatedTimeUtc]);
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PostViewDaily]') AND name = 'ViewDateUtc' AND system_type_id <> TYPE_ID(N'date'))
BEGIN
    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PostViewDaily]') AND name = N'IX_PostViewDaily_ViewDateUtc')
        DROP INDEX [IX_PostViewDaily_ViewDateUtc] ON [dbo].[PostViewDaily];

    IF EXISTS (SELECT * FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'[dbo].[PostViewDaily]') AND name = N'PK_PostViewDaily')
        ALTER TABLE [dbo].[PostViewDaily] DROP CONSTRAINT [PK_PostViewDaily];

    ALTER TABLE [dbo].[PostViewDaily] ALTER COLUMN [ViewDateUtc] [date] NOT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'[dbo].[PostViewDaily]') AND name = N'PK_PostViewDaily')
    ALTER TABLE [dbo].[PostViewDaily] ADD CONSTRAINT [PK_PostViewDaily] PRIMARY KEY ([PostId], [ViewDateUtc]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PostViewDaily]') AND name = N'IX_PostViewDaily_ViewDateUtc')
    CREATE INDEX [IX_PostViewDaily_ViewDateUtc] ON [dbo].[PostViewDaily]([ViewDateUtc]);
GO

IF EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[LoginHistory]'))
    DROP TABLE [dbo].[LoginHistory];
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SiteVerificationFile]') AND name = N'IX_SiteVerificationFile_NormalizedFileName')
BEGIN
    CREATE UNIQUE INDEX [IX_SiteVerificationFile_NormalizedFileName]
    ON [dbo].[SiteVerificationFile]([NormalizedFileName]);
END
GO
