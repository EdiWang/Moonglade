-- Supported upgrade baseline: Moonglade v16.3.0
CREATE TABLE [dbo].[ActivityLog](
    [Id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [EventId] [int] NOT NULL,
    [EventTimeUtc] [datetime] NULL,
    [ActorId] [nvarchar](100) NULL,
    [Operation] [nvarchar](100) NULL,
    [TargetName] [nvarchar](200) NULL,
    [MetaData] [nvarchar](max) NULL,
    [IpAddress] [nvarchar](50) NULL,
    [UserAgent] [nvarchar](512) NULL
);
GO

CREATE TABLE [dbo].[BlogAsset](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [LastModifiedTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[BlogConfiguration](
    [CfgKey] [nvarchar](64) NOT NULL PRIMARY KEY,
    [CfgValue] [nvarchar](max) NOT NULL,
    [LastModifiedTimeUtc] [datetime] NULL
);
GO

CREATE TABLE [dbo].[BlogPage](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [CreateTimeUtc] [datetime] NOT NULL,
    [UpdateTimeUtc] [datetime] NULL,
    [IsDeleted] [bit] NOT NULL
);
GO

CREATE TABLE [dbo].[Comment](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [CreateTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[CommentReply](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [CreateTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[EmailOutboxMessage](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [MessageType] [nvarchar](100) NOT NULL,
    [DistributionList] [nvarchar](4000) NOT NULL,
    [MessageBody] [nvarchar](max) NOT NULL,
    [Status] [int] NOT NULL,
    [AttemptCount] [int] NOT NULL,
    [CreatedTimeUtc] [datetime] NOT NULL,
    [LastAttemptTimeUtc] [datetime] NULL,
    [NotBeforeUtc] [datetime] NULL,
    [LockedUntilUtc] [datetime] NULL,
    [LockedBy] [nvarchar](128) NULL,
    [SentTimeUtc] [datetime] NULL,
    [LastError] [nvarchar](2000) NULL,
    [ConcurrencyToken] [uniqueidentifier] NOT NULL
);
GO

CREATE INDEX [IX_EmailOutboxMessage_Dequeue]
ON [dbo].[EmailOutboxMessage]([Status], [NotBeforeUtc], [LockedUntilUtc], [CreatedTimeUtc]);
GO

CREATE TABLE [dbo].[LoginHistory](
    [Id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [LoginTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[Mention](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [PingTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[Post](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [CreateTimeUtc] [datetime] NOT NULL,
    [PubDateUtc] [datetime] NULL,
    [LastModifiedUtc] [datetime] NULL,
    [ScheduledPublishTimeUtc] [datetime] NULL,
    [ContentType] [nvarchar](16) NOT NULL,
    [ContainsAiAssistedContent] [bit] NOT NULL
);
GO

CREATE TABLE [dbo].[PostView](
    [Id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [BeginTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[PostViewDaily](
    [PostId] [uniqueidentifier] NOT NULL,
    [ViewDateUtc] [datetime] NOT NULL,
    [ViewCount] [int] NOT NULL,
    CONSTRAINT [PK_PostViewDaily] PRIMARY KEY ([PostId], [ViewDateUtc])
);
GO

CREATE INDEX [IX_PostViewDaily_ViewDateUtc] ON [dbo].[PostViewDaily]([ViewDateUtc]);
GO

CREATE TABLE [dbo].[StyleSheet](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [LastModifiedTimeUtc] [datetime] NOT NULL
);
GO

CREATE TABLE [dbo].[Widget](
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [Title] [nvarchar](100) NOT NULL,
    [WidgetType] [nvarchar](50) NOT NULL,
    [ContentType] [nvarchar](25) NOT NULL,
    [ContentCode] [nvarchar](2000) NULL,
    [DisplayOrder] [int] NOT NULL,
    [IsEnabled] [bit] NOT NULL,
    [CreatedTimeUtc] [datetime] NOT NULL
);
GO

INSERT INTO [dbo].[Post] (
    [Id], [CreateTimeUtc], [PubDateUtc], [LastModifiedUtc], [ScheduledPublishTimeUtc], [ContentType], [ContainsAiAssistedContent]
) VALUES (
    '11111111-1111-1111-1111-111111111111', '2010-01-02T03:04:05.123', '2025-12-31T23:59:59.997', NULL, NULL, 'HTML', 0
);
GO

;WITH [Numbers] AS (
    SELECT TOP (339) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS [Number]
    FROM sys.all_objects AS [a]
    CROSS JOIN sys.all_objects AS [b]
)
INSERT INTO [dbo].[Post] (
    [Id], [CreateTimeUtc], [PubDateUtc], [LastModifiedUtc], [ScheduledPublishTimeUtc], [ContentType], [ContainsAiAssistedContent]
)
SELECT
    NEWID(),
    DATEADD(minute, -[Number], CONVERT(datetime, '2026-08-14T12:00:00')),
    CASE WHEN [Number] % 2 = 0 THEN DATEADD(minute, -[Number], CONVERT(datetime, '2026-08-14T12:00:00')) ELSE NULL END,
    NULL,
    NULL,
    'HTML',
    0
FROM [Numbers];
GO

;WITH [Numbers] AS (
    SELECT TOP (789) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS [Number]
    FROM sys.all_objects AS [a]
    CROSS JOIN sys.all_objects AS [b]
)
INSERT INTO [dbo].[Comment] ([Id], [CreateTimeUtc])
SELECT NEWID(), DATEADD(second, -[Number], CONVERT(datetime, '2026-08-14T12:00:00'))
FROM [Numbers];
GO

INSERT INTO [dbo].[PostViewDaily] ([PostId], [ViewDateUtc], [ViewCount])
VALUES ('11111111-1111-1111-1111-111111111111', '2026-08-13T00:00:00', 7);
GO

INSERT INTO [dbo].[EmailOutboxMessage] (
    [Id], [MessageType], [DistributionList], [MessageBody], [Status], [AttemptCount], [CreatedTimeUtc],
    [LastAttemptTimeUtc], [NotBeforeUtc], [LockedUntilUtc], [LockedBy], [SentTimeUtc], [LastError], [ConcurrencyToken]
) VALUES (
    '22222222-2222-2222-2222-222222222222', 'Test', 'test@example.com', 'fixture', 0, 0,
    '2026-08-14T01:02:03.123', NULL, '2026-08-14T01:05:00', NULL, NULL, NULL, NULL,
    '33333333-3333-3333-3333-333333333333'
);
GO

INSERT INTO [dbo].[BlogConfiguration] ([CfgKey], [CfgValue], [LastModifiedTimeUtc])
VALUES ('SystemManifestSettings', 'fixture', '2026-01-01T00:00:00');
GO
