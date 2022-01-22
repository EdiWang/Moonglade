INSERT INTO PostTag (PostId, TagId) (SELECT p.Id, t.Id FROM `Post` p LEFT JOIN `Tag` t ON 1 = 1);

INSERT INTO CustomPage(Id, Title, Slug, MetaDescription, HtmlContent, CssContent, HideSidebar, IsPublished, CreateTimeUtc, UpdateTimeUtc)
VALUES (UUID(), N'About', 'about', N'An Empty About Page', N'<h3>An Empty About Page</h3>', N'', 1, 1, NOW(), NOW());