CREATE TABLE Exceptions(
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
	`SQL` MEDIUMTEXT NULL,
	DeletionDate datetime NULL,
	FullJson MEDIUMTEXT NULL,
	ErrorHash int NULL,
	DuplicateCount int NOT NULL default 1,
 PRIMARY KEY (Id)
);

ALTER TABLE `Exceptions`
	ADD INDEX `IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate` (`GUID`, `ApplicationName`, `DeletionDate`, `CreationDate` desc);

ALTER TABLE `Exceptions`
	ADD INDEX `IX_Exceptions_ErrorHash_AppName_CreationDate_DelDate` (`ErrorHash`, `ApplicationName`, `CreationDate` desc, `DeletionDate`);



