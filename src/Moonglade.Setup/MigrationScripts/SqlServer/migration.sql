-- v16.7
-- Refuse to discard existing replies; the operator must reconcile orphans first.
IF EXISTS (SELECT 1 FROM [dbo].[CommentReply] r LEFT JOIN [dbo].[Comment] c ON c.[Id] = r.[CommentId]
           WHERE r.[CommentId] IS NULL OR c.[Id] IS NULL)
    THROW 51002, 'CommentReply contains orphan replies; reconcile them before migration.', 1;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[dbo].[CommentReply]') AND name = N'FK_CommentReply_Comment')
    ALTER TABLE [dbo].[CommentReply] DROP CONSTRAINT [FK_CommentReply_Comment];
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CommentReply]') AND name = N'CommentId' AND is_nullable = 1)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CommentReply]') AND name = N'IX_CommentReply_CommentId')
        DROP INDEX [IX_CommentReply_CommentId] ON [dbo].[CommentReply];
    ALTER TABLE [dbo].[CommentReply] ALTER COLUMN [CommentId] uniqueidentifier NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CommentReply]') AND name = N'IX_CommentReply_CommentId')
    CREATE INDEX [IX_CommentReply_CommentId] ON [dbo].[CommentReply]([CommentId]);
GO

ALTER TABLE [dbo].[CommentReply] WITH CHECK ADD CONSTRAINT [FK_CommentReply_Comment]
    FOREIGN KEY ([CommentId]) REFERENCES [dbo].[Comment]([Id]) ON DELETE CASCADE;
GO

UPDATE [dbo].[BlogConfiguration]
SET [CfgValue] = N'{"versionString":"16.8.1","installTimeUtc":"' +
        CONVERT(nvarchar(33), SYSUTCDATETIME(), 126) + N'Z"}',
    [LastModifiedTimeUtc] = SYSUTCDATETIME()
WHERE [CfgKey] = N'SystemManifestSettings';
GO
