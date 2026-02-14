-- v15.0
CREATE TABLE IF NOT EXISTS `Widget` (
    `Id` CHAR(36) NOT NULL,
    `Title` VARCHAR(100) NOT NULL,
    `WidgetType` VARCHAR(50) NOT NULL,
    `ContentType` VARCHAR(25) NOT NULL,
    `ContentCode` VARCHAR(2000) NULL,
    `DisplayOrder` INT NOT NULL,
    `IsEnabled` TINYINT(1) NOT NULL,
    `CreatedTimeUtc` DATETIME NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- v15.3
SET @dbname = DATABASE();
SET @tablename = 'Mention';
SET @columnname = 'Worker';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_SCHEMA = @dbname)
      AND (TABLE_NAME = @tablename)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  CONCAT('ALTER TABLE `', @tablename, '` DROP COLUMN `', @columnname, '`;'),
  'SELECT 1;'
));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- v15.4
SET @dbname = DATABASE();
SET @tablename = 'Post';
SET @columnname = 'HeroImageUrl';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_SCHEMA = @dbname)
      AND (TABLE_NAME = @tablename)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  CONCAT('ALTER TABLE `', @tablename, '` DROP COLUMN `', @columnname, '`;'),
  'SELECT 1;'
));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- v15.6
CREATE TABLE IF NOT EXISTS `ActivityLog` (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `EventId` INT NOT NULL,
    `EventTimeUtc` DATETIME NULL,
    `ActorId` VARCHAR(100) NULL,
    `Operation` VARCHAR(100) NULL,
    `TargetName` VARCHAR(200) NULL,
    `MetaData` TEXT NULL,
    `IpAddress` VARCHAR(50) NULL,
    `UserAgent` VARCHAR(512) NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
