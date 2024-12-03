-- v14.1
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'Rank' AND Object_ID = Object_ID(N'FriendLink'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE FriendLink ADD [Rank] INT'
    EXEC sp_executesql N'UPDATE FriendLink SET [Rank] = 0'
    EXEC sp_executesql N'ALTER TABLE FriendLink ALTER COLUMN [Rank] INT NOT NULL'
END

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


IF OBJECT_ID(N'[dbo].[LocalAccount]', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[LocalAccount]
END


IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'RouteName' AND Object_ID = Object_ID(N'Catery'))
BEGIN
    EXEC sys.sp_rename 
        @objname = N'Catery.RouteName', 
        @newname = 'Slug', 
        @objtype = 'COLUMN'
END


IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'Post' AND c.name = 'InlineCss'
)
BEGIN
    ALTER TABLE Post DROP COLUMN InlineCss;
END;


-- v14.5
IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'IsOriginal' AND Object_ID = Object_ID(N'Post'))
BEGIN
    ALTER TABLE Post DROP COLUMN IsOriginal
END


IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'OriginLink' AND Object_ID = Object_ID(N'Post'))
BEGIN
    ALTER TABLE Post DROP COLUMN OriginLink
END


IF OBJECT_ID(N'Mention', 'U') IS NULL AND OBJECT_ID(N'Pingback', 'U') IS NOT NULL
BEGIN
    EXEC sp_rename 'Pingback', 'Mention'
END


IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'Worker' AND Object_ID = Object_ID(N'Mention'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE Mention ADD Worker NVARCHAR(16)'
    EXEC sp_executesql N'UPDATE Mention SET Worker = N''Pingback'''
END

-- v14.8
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'RouteLink' AND Object_ID = Object_ID(N'Post'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE Post ADD RouteLink NVARCHAR(256)'
    EXEC sp_executesql N'UPDATE Post SET RouteLink = FORMAT(PubDateUtc, ''yyyy/M/d'') + ''/'' + Slug'
END

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'Post' AND c.name = 'HashCheckSum'
)
BEGIN
    ALTER TABLE Post DROP COLUMN HashCheckSum;
END

-- v14.15
UPDATE [BlogConfiguration] SET CfgKey = 'AppearanceSettings' WHERE CfgKey = 'CustomStyleSheetSettings';

-- Reset configuration keys
UPDATE BlogConfiguration SET Id = Id + 100 WHERE Id <> 99

UPDATE BlogConfiguration SET Id = 1 WHERE CfgKey = 'ContentSettings'
UPDATE BlogConfiguration SET Id = 2 WHERE CfgKey = 'NotificationSettings'
UPDATE BlogConfiguration SET Id = 3 WHERE CfgKey = 'FeedSettings'
UPDATE BlogConfiguration SET Id = 4 WHERE CfgKey = 'GeneralSettings'
UPDATE BlogConfiguration SET Id = 5 WHERE CfgKey = 'ImageSettings'
UPDATE BlogConfiguration SET Id = 6 WHERE CfgKey = 'AdvancedSettings'
UPDATE BlogConfiguration SET Id = 7 WHERE CfgKey = 'AppearanceSettings'
UPDATE BlogConfiguration SET Id = 8 WHERE CfgKey = 'CommentSettings'
UPDATE BlogConfiguration SET Id = 10 WHERE CfgKey = 'CustomMenuSettings'
UPDATE BlogConfiguration SET Id = 11 WHERE CfgKey = 'LocalAccountSettings'
UPDATE BlogConfiguration SET Id = 12 WHERE CfgKey = 'SocialLinkSettings'

DELETE FROM BlogConfiguration WHERE Id > 100
