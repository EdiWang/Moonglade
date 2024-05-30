-- v14.4.1 - v14.5.x

ALTER TABLE Post DROP COLUMN IsOriginal
GO

ALTER TABLE Post DROP COLUMN OriginLink
GO

EXEC sp_rename 'Pingback', 'Mention'
GO

ALTER TABLE Mention ADD Worker NVARCHAR(16)
GO

UPDATE Mention SET Worker = N'Pingback'
GO
