-- v14.4.1 - v14.5.x

EXEC sp_rename 'Pingback', 'Mention'
GO

ALTER TABLE Mention ADD Worker NVARCHAR(16)
GO

UPDATE Mention SET Worker = N'Pingback'
GO
