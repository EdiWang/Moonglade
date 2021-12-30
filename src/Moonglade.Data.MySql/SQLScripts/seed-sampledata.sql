INSERT INTO Category (Id, DisplayName, Note, RouteName) VALUES (NEWID(), 'Default', 'Default Category', 'default')
INSERT INTO Tag(DisplayName, NormalizedName) VALUES ('Moonglade', 'moonglade')
INSERT INTO Tag(DisplayName, NormalizedName) VALUES ('.NET Core', 'dotnet-core')
INSERT INTO FriendLink (Id, LinkUrl, Title) VALUES (NEWID(), 'https://edi.wang', 'Edi.Wang')
INSERT INTO [Menu]([Id], [Title], [Url], [Icon], [DisplayOrder], [IsOpenInNewTab]) VALUES (NEWID(), 'About', '/page/about', 'icon-star-full', '0', '0')

DECLARE @CatId UNIQUEIDENTIFIER
SELECT TOP 1 @CatId = Id FROM Category

DECLARE @NewPostId UNIQUEIDENTIFIER
SET @NewPostId = NEWID()

DECLARE @PostCotent NVARCHAR(MAX)
SET @PostCotent = N'Moonglade is the new blog system for https://edi.wang. It is a complete rewrite of the old system using .NET 5 and runs on Microsoft Azure.'

INSERT INTO Post(Id, Title, Slug, Author, PostContent, CommentEnabled, CreateTimeUtc, ContentAbstract, IsPublished, IsFeatured, IsFeedIncluded, LastModifiedUtc, IsDeleted, PubDateUtc, ContentLanguageCode, HashCheckSum, IsOriginal) 
VALUES (@NewPostId, 'Welcome to Moonglade', 'welcome-to-moonglade', 'admin', @PostCotent, 1, '2021-1-1', @PostCotent, 1, 0, 1, NULL, 0, GETDATE(), 'en-us', -1688639577, 1)

INSERT INTO PostExtension(PostId,  Hits,  Likes) 
VALUES (@NewPostId,  1024,  512)

INSERT INTO PostCategory (PostId, CategoryId) VALUES (@NewPostId, @CatId)
INSERT INTO PostTag (PostId, TagId) (SELECT p.Id, t.Id FROM Post p LEFT JOIN Tag t ON 1 = 1)

INSERT INTO CustomPage(Id, Title, Slug, MetaDescription, HtmlContent, CssContent, HideSidebar, IsPublished, CreateTimeUtc, UpdateTimeUtc)
VALUES (NEWID(), N'About', 'about', N'An Empty About Page', N'<h3>An Empty About Page</h3>', N'', 1, 1, GETDATE(), GETDATE())