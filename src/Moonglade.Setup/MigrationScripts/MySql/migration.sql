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
