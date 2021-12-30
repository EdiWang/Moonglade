IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'LocalAccount')
CREATE TABLE [LocalAccount](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Username] [varchar](32) NOT NULL,
[PasswordHash] [nvarchar](64) NOT NULL,
[LastLoginTimeUtc] [datetime] NULL,
[LastLoginIp] [nvarchar](64) NULL,
[CreateTimeUtc] [datetime] NOT NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'BlogConfiguration')
CREATE TABLE [BlogConfiguration](
[Id] [int] PRIMARY KEY CLUSTERED NOT NULL,
[CfgKey] [varchar](64) NULL,
[CfgValue] [nvarchar](max) NULL,
[LastModifiedTimeUtc] [datetime] NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'BlogAsset')
CREATE TABLE BlogAsset(
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Base64Data] [nvarchar](max) NULL,
[LastModifiedTimeUtc] [datetime] NOT NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Category')
CREATE TABLE [Category](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[RouteName] [nvarchar](64) NULL,
[DisplayName] [nvarchar](64) NULL,
[Note] [nvarchar](128) NULL) 

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Comment')
CREATE TABLE [Comment](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Username] [nvarchar](64) NULL,
[Email] [nvarchar](128) NULL,
[IPAddress] [nvarchar](64) NULL,
[CreateTimeUtc] [datetime] NOT NULL,
[CommentContent] [nvarchar](max) NOT NULL,
[PostId] [uniqueidentifier] NOT NULL,
[IsApproved] [bit] NOT NULL) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CommentReply')
CREATE TABLE [CommentReply](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[ReplyContent] [nvarchar](max) NULL,
[CreateTimeUtc] [datetime] NOT NULL,
[CommentId] [uniqueidentifier] NULL) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'FriendLink')
CREATE TABLE [FriendLink](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Title] [nvarchar](64) NULL,
[LinkUrl] [nvarchar](256) NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Pingback')
CREATE TABLE [Pingback](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Domain] [nvarchar](256) NULL,
[SourceUrl] [nvarchar](256) NULL,
[SourceTitle] [nvarchar](256) NULL,
[SourceIp] [nvarchar](64) NULL,
[TargetPostId] [uniqueidentifier] NOT NULL,
[PingTimeUtc] [datetime] NOT NULL,
[TargetPostTitle] [nvarchar](128) NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Post')
CREATE TABLE [Post](
[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Title] [nvarchar](128) NULL,
[Slug] [nvarchar](128) NULL,
[Author] [nvarchar](64) NULL,
[PostContent] [nvarchar](max) NULL,
[CommentEnabled] [bit] NOT NULL,
[CreateTimeUtc] [datetime] NOT NULL,
[ContentAbstract] [nvarchar](1024) NULL,
[ContentLanguageCode] [nvarchar](8) NULL,
[IsFeedIncluded] [bit] NOT NULL,
[PubDateUtc] [datetime] NULL,
[LastModifiedUtc] [datetime] NULL,
[IsPublished] [bit] NOT NULL,
[IsFeatured] [bit] NOT NULL,
[IsOriginal] [bit] NOT NULL,
[OriginLink] [nvarchar](256) NULL,
[HeroImageUrl] [nvarchar](256) NULL,
[InlineCss] [nvarchar](2048) NULL,
[IsDeleted] [bit] NOT NULL,
[HashCheckSum] [int] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PostCategory')
CREATE TABLE [PostCategory](
	[PostId] [uniqueidentifier] NOT NULL,
	[CategoryId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_PostCategory] PRIMARY KEY CLUSTERED 
(
	[PostId] ASC,
	[CategoryId] ASC
) ON [PRIMARY]
) ON [PRIMARY]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PostExtension')
CREATE TABLE [PostExtension](
[PostId] [uniqueidentifier] PRIMARY KEY NOT NULL,
[Hits] [int] NOT NULL,
[Likes] [int] NOT NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'PostTag')
CREATE TABLE [PostTag](
	[PostId] [uniqueidentifier] NOT NULL,
	[TagId] [int] NOT NULL,
 CONSTRAINT [PK_PostTag] PRIMARY KEY CLUSTERED 
(
	[PostId] ASC,
	[TagId] ASC
) ON [PRIMARY]
) ON [PRIMARY]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Tag')
CREATE TABLE [Tag](
[Id] [int] IDENTITY(1,1) PRIMARY KEY CLUSTERED NOT NULL,
[DisplayName] [nvarchar](32) NULL,
[NormalizedName] [nvarchar](32) NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CustomPage')
CREATE TABLE [CustomPage](
	[Id] UNIQUEIDENTIFIER PRIMARY KEY NOT NULL,
	[Title] NVARCHAR(128) NULL,
	[Slug] NVARCHAR(128) NULL,
	[MetaDescription] NVARCHAR(256) NULL,
	[HtmlContent] NVARCHAR(MAX) NULL,
	[CssContent] NVARCHAR(MAX) NULL,
	[HideSidebar] BIT NOT NULL,
	[IsPublished] BIT NOT NULL,
	[CreateTimeUtc] DATETIME NOT NULL,
	[UpdateTimeUtc] DATETIME NULL
)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Menu')
CREATE TABLE [Menu](
	[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
	[Title] [nvarchar](64) NOT NULL,
	[Url] [nvarchar](256) NOT NULL,
	[Icon] [nvarchar](64) NULL,
	[DisplayOrder] INT NOT NULL,
	[IsOpenInNewTab] [bit] NOT NULL
)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SubMenu')
CREATE TABLE [SubMenu](
	[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,
	[Title] [nvarchar](64) NOT NULL,
	[Url] [nvarchar](256) NOT NULL,
	[IsOpenInNewTab] [bit] NOT NULL,
	[MenuId] [uniqueidentifier] NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'BlogTheme')
CREATE TABLE [BlogTheme](
[Id] [int] PRIMARY KEY CLUSTERED NOT NULL IDENTITY(1,1),
[ThemeName] [varchar](32) NULL,
[CssRules] [nvarchar](max) NULL,
[AdditionalProps] [nvarchar](max) NULL,
[ThemeType] [int] NOT NULL)

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_Comment_Post')
ALTER TABLE [Comment] WITH CHECK ADD CONSTRAINT [FK_Comment_Post] FOREIGN KEY([PostId])
REFERENCES [Post] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
ALTER TABLE [Comment] CHECK CONSTRAINT [FK_Comment_Post]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_PostCategory_Category')
ALTER TABLE [PostCategory]  WITH CHECK ADD  CONSTRAINT [FK_PostCategory_Category] FOREIGN KEY([CategoryId])
REFERENCES [Category] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
ALTER TABLE [PostCategory] CHECK CONSTRAINT [FK_PostCategory_Category]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_PostCategory_Post')
ALTER TABLE [PostCategory]  WITH CHECK ADD  CONSTRAINT [FK_PostCategory_Post] FOREIGN KEY([PostId])
REFERENCES [Post] ([Id])
ON DELETE CASCADE
ALTER TABLE [PostCategory] CHECK CONSTRAINT [FK_PostCategory_Post]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_PostExtension_Post')
ALTER TABLE [PostExtension]  WITH CHECK ADD  CONSTRAINT [FK_PostExtension_Post] FOREIGN KEY([PostId])
REFERENCES [Post] ([Id])
ON DELETE CASCADE
ALTER TABLE [PostExtension] CHECK CONSTRAINT [FK_PostExtension_Post]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_PostTag_Post')
ALTER TABLE [PostTag]  WITH CHECK ADD  CONSTRAINT [FK_PostTag_Post] FOREIGN KEY([PostId])
REFERENCES [Post] ([Id])
ON DELETE CASCADE
ALTER TABLE [PostTag] CHECK CONSTRAINT [FK_PostTag_Post]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_PostTag_Tag')
ALTER TABLE [PostTag]  WITH CHECK ADD  CONSTRAINT [FK_PostTag_Tag] FOREIGN KEY([TagId])
REFERENCES [Tag] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
ALTER TABLE [PostTag] CHECK CONSTRAINT [FK_PostTag_Tag]

IF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_SubMenu_Menu')
ALTER TABLE [SubMenu] WITH CHECK ADD CONSTRAINT [FK_SubMenu_Menu] FOREIGN KEY([MenuId])
REFERENCES [Menu] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
ALTER TABLE [SubMenu] CHECK CONSTRAINT [FK_SubMenu_Menu]