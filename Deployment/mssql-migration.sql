-- v14.1
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'Rank' AND Object_ID = Object_ID(N'FriendLink'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE FriendLink ADD [Rank] INT'
    EXEC sp_executesql N'UPDATE FriendLink SET [Rank] = 0'
    EXEC sp_executesql N'ALTER TABLE FriendLink ALTER COLUMN [Rank] INT NOT NULL'
END
GO

-- v14.3
IF NOT EXISTS (SELECT * FROM sys.objects 
               WHERE object_id = OBJECT_ID(N'[dbo].[LoginHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LoginHistory](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LoginTimeUtc] [datetime] NOT NULL,
        [LoginIp] [nvarchar](64) NULL,
        [LoginUserAgent] [nvarchar](128) NULL,
        [DeviceFingerprint] [nvarchar](128) NULL,
     CONSTRAINT [PK_LoginHistory] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
END
GO

IF OBJECT_ID(N'[dbo].[LocalAccount]', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[LocalAccount]
END
GO

IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'RouteName' AND Object_ID = Object_ID(N'Category'))
BEGIN
    EXEC sys.sp_rename 
        @objname = N'Category.RouteName', 
        @newname = 'Slug', 
        @objtype = 'COLUMN'
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'Post' AND c.name = 'InlineCss'
)
BEGIN
    ALTER TABLE Post DROP COLUMN InlineCss;
END;
GO

-- v14.5
IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'IsOriginal' AND Object_ID = Object_ID(N'Post'))
BEGIN
    ALTER TABLE Post DROP COLUMN IsOriginal
END
GO

IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'OriginLink' AND Object_ID = Object_ID(N'Post'))
BEGIN
    ALTER TABLE Post DROP COLUMN OriginLink
END
GO

IF OBJECT_ID(N'Mention', 'U') IS NULL AND OBJECT_ID(N'Pingback', 'U') IS NOT NULL
BEGIN
    EXEC sp_rename 'Pingback', 'Mention'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'Worker' AND Object_ID = Object_ID(N'Mention'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE Mention ADD Worker NVARCHAR(16)'
    EXEC sp_executesql N'UPDATE Mention SET Worker = N''Pingback'''
END
GO

-- v14.8
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'RouteLink' AND Object_ID = Object_ID(N'Post'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE Post ADD RouteLink NVARCHAR(256)'
    EXEC sp_executesql N'UPDATE Post SET RouteLink = FORMAT(PubDateUtc, ''yyyy/M/d'') + ''/'' + Slug'
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'Post' AND c.name = 'HashCheckSum'
)
BEGIN
    ALTER TABLE Post DROP COLUMN HashCheckSum;
END
GO

-- v14.15
UPDATE [BlogConfiguration] SET CfgKey = 'AppearanceSettings' WHERE CfgKey = 'CustomStyleSheetSettings';
GO

ALTER TABLE [dbo].[BlogConfiguration] ALTER COLUMN [CfgKey] VARCHAR(64) NOT NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[PK_BlogConfiguration]')
          AND type = 'PK'
)
BEGIN
    ALTER TABLE [dbo].[BlogConfiguration] DROP CONSTRAINT [PK_BlogConfiguration];
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints kc
    INNER JOIN sys.tables t ON kc.parent_object_id = t.object_id
    WHERE kc.type = 'PK'
      AND t.name = 'BlogConfiguration'
)
BEGIN
    ALTER TABLE [dbo].[BlogConfiguration] ADD CONSTRAINT [PK_BlogConfiguration_CfgKey] PRIMARY KEY CLUSTERED ([CfgKey] ASC);
END
GO

IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BlogConfiguration'
      AND COLUMN_NAME = 'Id'
)
BEGIN
    ALTER TABLE [dbo].[BlogConfiguration] DROP COLUMN Id;
END
GO

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
