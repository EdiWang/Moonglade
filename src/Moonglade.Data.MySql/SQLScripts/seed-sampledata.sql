INSERT INTO Tag(DisplayName, NormalizedName) VALUES ('Moonglade', 'moonglade');
INSERT INTO Tag(DisplayName, NormalizedName) VALUES ('.NET Core', 'dotnet-core');
INSERT INTO FriendLink (Id, LinkUrl, Title) VALUES (UUID(), 'https://edi.wang', 'Edi.Wang');
INSERT INTO Menu(Id, Title, Url, Icon, DisplayOrder, IsOpenInNewTab) VALUES (UUID(), 'About', '/page/about', 'icon-star-full', '0', '0');

INSERT INTO PostTag (PostId, TagId) (SELECT p.Id, t.Id FROM `Post` p LEFT JOIN `Tag` t ON 1 = 1);

INSERT INTO CustomPage(Id, Title, Slug, MetaDescription, HtmlContent, CssContent, HideSidebar, IsPublished, CreateTimeUtc, UpdateTimeUtc)
VALUES (UUID(), N'About', 'about', N'An Empty About Page', N'<h3>An Empty About Page</h3>', N'', 1, 1, NOW(), NOW());