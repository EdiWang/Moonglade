SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[BlogConfiguration](
	[Id] [int] NOT NULL,
	[CfgKey] [varchar](64) NULL,
	[CfgValue] [nvarchar](max) NULL,
	[LastModifiedTimeUtc] [datetime] NULL,
 CONSTRAINT [PK_BlogConfiguration] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[Category](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](64) NULL,
	[DisplayName] [nvarchar](64) NULL,
	[Note] [nvarchar](128) NULL,
 CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[Comment](
	[Id] [uniqueidentifier] NOT NULL,
	[Username] [nvarchar](64) NULL,
	[Email] [nvarchar](128) NULL,
	[IPAddress] [nvarchar](64) NULL,
	[CreateOnUtc] [datetime] NOT NULL,
	[CommentContent] [nvarchar](max) NOT NULL,
	[PostId] [uniqueidentifier] NOT NULL,
	[IsApproved] [bit] NOT NULL,
	[UserAgent] [nvarchar](512) NULL,
 CONSTRAINT [PK_Comment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[CommentReply](
	[Id] [uniqueidentifier] NOT NULL,
	[ReplyContent] [nvarchar](max) NULL,
	[ReplyTimeUtc] [datetime] NULL,
	[UserAgent] [nvarchar](512) NULL,
	[IpAddress] [nvarchar](64) NULL,
	[CommentId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_CommentReply] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[FriendLink](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](64) NULL,
	[LinkUrl] [nvarchar](256) NULL,
 CONSTRAINT [PK_FriendLink] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[PingbackHistory](
	[Id] [uniqueidentifier] NOT NULL,
	[Domain] [nvarchar](256) NULL,
	[SourceUrl] [nvarchar](256) NULL,
	[SourceTitle] [nvarchar](256) NULL,
	[SourceIp] [nvarchar](64) NULL,
	[TargetPostId] [uniqueidentifier] NULL,
	[PingTimeUtc] [datetime] NULL,
	[Direction] [nvarchar](16) NULL,
	[TargetPostTitle] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[Post](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](128) NULL,
	[Slug] [nvarchar](128) NULL,
	[PostContent] [nvarchar](max) NULL,
	[CommentEnabled] [bit] NOT NULL,
	[CreateOnUtc] [datetime] NULL,
	[ContentAbstract] [nvarchar](1024) NULL,
 CONSTRAINT [PK_Post] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[PostCategory](
	[PostId] [uniqueidentifier] NOT NULL,
	[CategoryId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_PostCategory] PRIMARY KEY CLUSTERED 
(
	[PostId] ASC,
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[PostExtension](
	[PostId] [uniqueidentifier] NOT NULL,
	[Hits] [int] NOT NULL,
	[Likes] [int] NOT NULL,
 CONSTRAINT [PK_PostExtension] PRIMARY KEY CLUSTERED 
(
	[PostId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[PostPublish](
	[PostId] [uniqueidentifier] NOT NULL,
	[IsPublished] [bit] NOT NULL,
	[ExposedToSiteMap] [bit] NOT NULL,
	[IsFeedIncluded] [bit] NOT NULL,
	[LastModifiedUtc] [datetime] NULL,
	[IsDeleted] [bit] NOT NULL,
	[PubDateUtc] [datetime] NULL,
	[Revision] [int] NULL,
	[PublisherIp] [nvarchar](64) NULL,
	[ContentLanguageCode] [nvarchar](8) NULL,
PRIMARY KEY CLUSTERED 
(
	[PostId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[PostTag](
	[PostId] [uniqueidentifier] NOT NULL,
	[TagId] [int] NOT NULL,
 CONSTRAINT [PK_PostTag] PRIMARY KEY CLUSTERED 
(
	[PostId] ASC,
	[TagId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

SET ANSI_NULLS ON
-- GO
SET QUOTED_IDENTIFIER ON
-- GO
CREATE TABLE [dbo].[Tag](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DisplayName] [nvarchar](32) NULL,
	[NormalizedName] [nvarchar](32) NULL,
 CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-- GO

ALTER TABLE [dbo].[Comment]  WITH CHECK ADD  CONSTRAINT [FK_Comment_Post] FOREIGN KEY([PostId])
REFERENCES [dbo].[Post] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[Comment] CHECK CONSTRAINT [FK_Comment_Post]
-- GO
ALTER TABLE [dbo].[PostCategory]  WITH CHECK ADD  CONSTRAINT [FK_PostCategory_Category] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Category] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[PostCategory] CHECK CONSTRAINT [FK_PostCategory_Category]
-- GO
ALTER TABLE [dbo].[PostCategory]  WITH CHECK ADD  CONSTRAINT [FK_PostCategory_Post] FOREIGN KEY([PostId])
REFERENCES [dbo].[Post] ([Id])
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[PostCategory] CHECK CONSTRAINT [FK_PostCategory_Post]
-- GO
ALTER TABLE [dbo].[PostExtension]  WITH CHECK ADD  CONSTRAINT [FK_PostExtension_Post] FOREIGN KEY([PostId])
REFERENCES [dbo].[Post] ([Id])
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[PostExtension] CHECK CONSTRAINT [FK_PostExtension_Post]
-- GO
ALTER TABLE [dbo].[PostPublish]  WITH CHECK ADD  CONSTRAINT [FK_PostPublish_Post] FOREIGN KEY([PostId])
REFERENCES [dbo].[Post] ([Id])
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[PostPublish] CHECK CONSTRAINT [FK_PostPublish_Post]
-- GO
ALTER TABLE [dbo].[PostTag]  WITH CHECK ADD  CONSTRAINT [FK_PostTag_Post] FOREIGN KEY([PostId])
REFERENCES [dbo].[Post] ([Id])
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[PostTag] CHECK CONSTRAINT [FK_PostTag_Post]
-- GO
ALTER TABLE [dbo].[PostTag]  WITH CHECK ADD  CONSTRAINT [FK_PostTag_Tag] FOREIGN KEY([TagId])
REFERENCES [dbo].[Tag] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
-- GO
ALTER TABLE [dbo].[PostTag] CHECK CONSTRAINT [FK_PostTag_Tag]
-- GO