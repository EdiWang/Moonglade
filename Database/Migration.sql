--INSERT BlogConfiguration (Id, CfgKey, CfgValue, LastModifiedTimeUtc) VALUES (22, 'BloggerAvatarBase64', N'', GETDATE())

ALTER TABLE BlogConfiguration ALTER COLUMN CfgValue NVARCHAR(MAX)
GO