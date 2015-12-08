CREATE TABLE Exceptions
(
	Id SERIAL NOT NULL,
	GUID char(36) NOT NULL,
	ApplicationName varchar(50) NOT NULL,
	MachineName varchar(50) NOT NULL,
	CreationDate TIMESTAMP(3) NOT NULL,
	Type varchar(100) NOT NULL,
	IsProtected SMALLINT NOT NULL default 0,
	Host varchar(100) NULL,
	Url varchar(500) NULL,
	HTTPMethod varchar(10) NULL,
	IPAddress varchar(40) NULL,
	Source varchar(100) NULL,
	Message varchar(1000) NULL,
	Detail TEXT NULL,	
	StatusCode int NULL,
	SQL TEXT NULL,
	DeletionDate TIMESTAMP(3) NULL,
	FullJson TEXT NULL,
	ErrorHash int NULL,
	DuplicateCount int NOT NULL default 1,
 PRIMARY KEY (Id)
);

/* TODO: Create indexes. */