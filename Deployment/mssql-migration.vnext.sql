-- v14.8
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE Name = N'RouteLink' AND Object_ID = Object_ID(N'Post'))
BEGIN
    ALTER TABLE Post ADD RouteLink NVARCHAR(256)
    UPDATE Post SET RouteLink = FORMAT(PubDateUtc, 'yyyy/M/d') + '/' + Slug
END

