DELETE FROM BlogConfiguration

INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (1, 'ContentSettings', '{"EnableComments":true,"RequireCommentReview":true,"EnableWordFilter":false,"PostListPageSize":10,"HotTagAmount":10,"DisharmonyWords":"fuck|shit","ShowCalloutSection":false,"CalloutSectionHtmlPitch":""}', GETDATE())
INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (2, 'NotificationSettings', '{"EnableEmailSending":true,"EnableSsl":true,"SendEmailOnCommentReply":true,"SendEmailOnNewComment":true,"SmtpServerPort":587,"AdminEmail":"","EmailDisplayName":"Moonglade","SmtpPassword":"","SmtpServer":"","SmtpUserName":"","BannedMailDomain":""}', GETDATE())
INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (3, 'FeedSettings', '{"RssItemCount":20,"RssCopyright":"(c) {year} Moonglade","RssDescription":"Latest posts from Moonglade","RssTitle":"Moonglade","AuthorName":"Admin","UseFullContent":false}', GETDATE())
INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (4, 'GeneralSettings', '{"OwnerName":"Admin","OwnerEmail":"admin@edi.wang","Description":"Moonglade Admin","ShortDescription":"Moonglade Admin","AvatarBase64":"","SiteTitle":"Moonglade","LogoText":"moonglade","MetaKeyword":"moonglade","MetaDescription":"Just another .NET blog system","Copyright":"[c] 2021","SideBarCustomizedHtmlPitch":"","FooterCustomizedHtmlPitch":"","UserTimeZoneBaseUtcOffset":"08:00:00","TimeZoneId":"China Standard Time","AutoDarkLightTheme":true,"ThemeId":1}', GETDATE())
INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (5, 'ImageSettings', '{"IsWatermarkEnabled":true,"KeepOriginImage":false,"WatermarkFontSize":20,"WatermarkText":"Moonglade","UseFriendlyNotFoundImage":true}', GETDATE())
INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (6, 'AdvancedSettings', '{"DNSPrefetchEndpoint":"","EnableOpenGraph":true,"EnablePingBackSend":true,"EnablePingBackReceive":true,"EnableOpenSearch":true,"WarnExternalLink":true,"AllowScriptsInPage":false,"ShowAdminLoginButton":false,"EnablePostRawEndpoint":true}', GETDATE())
INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (7, 'CustomStyleSheetSettings', '{"EnableCustomCss":false,"CssCode":""}', GETDATE())

DELETE FROM LocalAccount
INSERT LocalAccount(Id, Username, PasswordHash, CreateTimeUtc) VALUES (NEWID(), 'admin', 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=', GETDATE())

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('Word Blue', '{"--accent-color1": "#2a579a","--accent-color2": "#1a365f","--accent-color3": "#3e6db5"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('Excel Green', '{"--accent-color1": "#165331","--accent-color2": "#0E351F","--accent-color3": "#0E703A"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('PowerPoint Orange', '{"--accent-color1": "#983B22","--accent-color2": "#622616","--accent-color3": "#C43E1C"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('OneNote Purple', '{"--accent-color1": "#663276","--accent-color2": "#52285E","--accent-color3": "#7719AA"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('Outlook Blue', '{"--accent-color1": "#035AA6","--accent-color2": "#032B51","--accent-color3": "#006CBF"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('China Red', '{"--accent-color1": "#800900","--accent-color2": "#5d120d","--accent-color3": "#c5170a"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('Indian Curry', '{"--accent-color1": "rgb(128 84 3)","--accent-color2": "rgb(95 62 0)","--accent-color3": "rgb(208 142 19)"}', 0)

INSERT INTO BlogTheme
    (ThemeName, CssRules, ThemeType)
VALUES
    ('Metal Blue', '{"--accent-color1": "#4E5967","--accent-color2": "#333942","--accent-color3": "#6e7c8e"}', 0)