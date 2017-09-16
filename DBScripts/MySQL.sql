/* 
    MySQL setup script for Exceptional
    Run this script for creating the exceptions table
    It will also upgrade a V1 schema to V2, just run the full script.
*/

CREATE TABLE IF NOT EXISTS Exceptions(
    Id bigint NOT NULL AUTO_INCREMENT,
    GUID char(36) NOT NULL,
    ApplicationName nvarchar(50) NOT NULL,
    MachineName nvarchar(50) NOT NULL,
    CreationDate datetime NOT NULL,
    `Type` nvarchar(100) NOT NULL,
    IsProtected tinyint(1) NOT NULL default 0,
    Host nvarchar(100) NULL,
    Url nvarchar(500) NULL,
    HTTPMethod nvarchar(10) NULL,
    IPAddress varchar(40) NULL,
    Source nvarchar(100) NULL,
    Message nvarchar(1000) NULL,
    Detail MEDIUMTEXT NULL,	
    StatusCode int NULL,
    DeletionDate datetime NULL,
    FullJson MEDIUMTEXT NULL,
    ErrorHash int NULL,
    DuplicateCount int NOT NULL default 1,
    LastLogDate datetime NULL,
    Category nvarchar(100) NULL,
    PRIMARY KEY (Id)
);

SELECT IF (EXISTS(SELECT 1 
                    FROM INFORMATION_SCHEMA.STATISTICS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'Exceptions'
                     AND INDEX_NAME = 'IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate')
          ,'Select ''Already There'''
          ,'CREATE INDEX `IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate` ON `Exceptions`(`GUID`, `ApplicationName`, `DeletionDate`, `CreationDate` DESC);')
  INTO @a;
PREPARE q1 FROM @a;
EXECUTE q1;
DEALLOCATE PREPARE q1;

SELECT IF (EXISTS(SELECT 1 
                    FROM INFORMATION_SCHEMA.STATISTICS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'Exceptions'
                     AND INDEX_NAME = 'IX_Exceptions_ErrorHash_AppName_CreationDate_DelDate')
          ,'Select ''Already There'''
          ,'CREATE INDEX `IX_Exceptions_ErrorHash_AppName_CreationDate_DelDate` ON `Exceptions`(`ErrorHash`, `ApplicationName`, `CreationDate` DESC, `DeletionDate`);')
  INTO @a;
PREPARE q1 FROM @a;
EXECUTE q1;
DEALLOCATE PREPARE q1;

/* Begin V2 Schema changes */

SELECT IF (EXISTS(SELECT 1 
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'Exceptions'
                     AND COLUMN_NAME = 'LastLogDate')
          ,'Select ''Already There'''
          ,'ALTER TABLE `Exceptions` ADD LastLogDate datetime NULL;')
  INTO @a;
PREPARE q1 FROM @a;
EXECUTE q1;
DEALLOCATE PREPARE q1;

SELECT IF (EXISTS(SELECT 1 
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'Exceptions'
                     AND COLUMN_NAME = 'Category')
          ,'Select ''Already There'''
          ,'ALTER TABLE `Exceptions` ADD Category nvarchar(100) NULL;')
  INTO @a;
PREPARE q1 FROM @a;
EXECUTE q1;
DEALLOCATE PREPARE q1;

SELECT IF (EXISTS(SELECT 1 
                    FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = 'Exceptions'
                     AND COLUMN_NAME = 'SQL')
          ,'ALTER TABLE `Exceptions` DROP COLUMN `SQL`;'
          ,'Select ''Already Gone''')
  INTO @a;
PREPARE q1 FROM @a;
EXECUTE q1;
DEALLOCATE PREPARE q1;