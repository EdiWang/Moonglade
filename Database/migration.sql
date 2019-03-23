-- Drop Default Values
ALTER TABLE [dbo].[PostExtension] DROP CONSTRAINT [DF_PostExtension_Hits]
ALTER TABLE [dbo].[PostPublish] DROP CONSTRAINT [DF__PostPubli__IsDel__5FB337D6]
GO

-- Change Data Type
ALTER TABLE PostExtension ALTER COLUMN Likes INT NOT NULL
GO