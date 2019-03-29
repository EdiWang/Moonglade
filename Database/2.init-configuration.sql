INSERT BlogConfiguration VALUES (1, N'DisharmonyWords', N'fuck|shit', GETDATE())
INSERT BlogConfiguration VALUES (2, N'MetaKeyword', N'Moonglade', GETDATE())
INSERT BlogConfiguration VALUES (3, N'MetaAuthor', N'Admin', GETDATE())
INSERT BlogConfiguration VALUES (4, N'SiteTitle', N'Moonglade', GETDATE())
INSERT BlogConfiguration VALUES (5, N'BloggerAvatarBase64', N'', GETDATE())
INSERT BlogConfiguration VALUES (6, N'EnableComments', N'True', GETDATE())
GO

INSERT BlogConfiguration VALUES (100, 'FeedSettings', '{"RssItemCount":20,"RssCopyright":"(c) {year} Moonglade","RssDescription":"Latest posts from Moonglade","RssGeneratorName":"Moonglade","RssTitle":"Moonglade","AuthorName":"Admin"}', GETDATE())
INSERT BlogConfiguration VALUES (200, 'WatermarkSettings', '{"IsEnabled":true,"KeepOriginImage":false,"FontSize":20,"WatermarkText":"Moonglade"}', GETDATE())
INSERT BlogConfiguration VALUES (300, 'EmailConfiguration', '{"EnableEmailSending":true,"EnableSsl":true,"SendEmailOnCommentReply":true,"SendEmailOnNewComment":true,"SmtpServerPort":587,"AdminEmail":"","EmailDisplayName":"Moonglade","SmtpPassword":"","SmtpServer":"","SmtpUserName":"","BannedMailDomain":""}', GETDATE())

GO