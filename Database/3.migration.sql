-- For Production Azure SQL Database
ALTER TABLE Comment ALTER COLUMN IsApproved BIT NOT NULL
ALTER TABLE Post ALTER COLUMN CommentEnabled BIT NOT NULL