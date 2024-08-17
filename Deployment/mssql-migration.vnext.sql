-- v14.8
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'RouteLink' AND Object_ID = Object_ID(N'Post'))
BEGIN
    EXEC sp_executesql N'ALTER TABLE Post ADD RouteLink NVARCHAR(256)'
    EXEC sp_executesql N'UPDATE Post SET RouteLink = FORMAT(PubDateUtc, ''yyyy/M/d'') + ''/'' + Slug'
END

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON c.object_id = o.object_id
    WHERE o.name = 'Post' AND c.name = 'HashCheckSum'
)
BEGIN
    ALTER TABLE Post DROP COLUMN HashCheckSum;
END;