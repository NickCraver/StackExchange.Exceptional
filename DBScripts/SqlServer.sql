/* 
    SQL Server setup script for Exceptional
    Run this script for creating the exceptions table
    It will also upgrade a V1 schema to V2, just run the full script.
*/
IF NOT EXISTS (SELECT 1 
                 FROM INFORMATION_SCHEMA.TABLES 
                WHERE [TABLE_SCHEMA] = 'dbo'
                  AND [TABLE_NAME] = 'Exceptions')
BEGIN
CREATE TABLE [dbo].[Exceptions](
    [Id] bigint NOT NULL IDENTITY,
    [GUID] uniqueidentifier NOT NULL,
    [ApplicationName] nvarchar(50) NOT NULL,
    [MachineName] nvarchar(50) NOT NULL,
    [CreationDate] datetime NOT NULL,
    [Type] nvarchar(100) NOT NULL,
    [IsProtected] bit NOT NULL DEFAULT(0),
    [Host] nvarchar(100) NULL,
    [Url] nvarchar(500) NULL,
    [HTTPMethod] nvarchar(10) NULL,
    [IPAddress] varchar(40) NULL,
    [Source] nvarchar(100) NULL,
    [Message] nvarchar(1000) NULL,
    [Detail] nvarchar(max) NULL,
    [StatusCode] int NULL,
    [DeletionDate] datetime NULL,
    [FullJson] nvarchar(max) NULL,
    [ErrorHash] int NULL,
    [DuplicateCount] int NOT NULL DEFAULT(1),
    [LastLogDate] datetime NULL,
    [Category] nvarchar(100) NULL,
    [LogLevel] tinyint NOT NULL
    CONSTRAINT [PK_Exceptions] PRIMARY KEY Clustered ([Id] ASC)

    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Exceptions') AND [name] = 'IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate] ON [dbo].[Exceptions] 
    (
        [GUID] ASC,
        [ApplicationName] ASC,
        [DeletionDate] ASC,
        [CreationDate] DESC
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Exceptions') AND [name] = 'IX_Exceptions_ErrorHash_ApplicationName_CreationDate_DeletionDate')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Exceptions_ErrorHash_ApplicationName_CreationDate_DeletionDate] ON [dbo].[Exceptions] 
    (
        [ErrorHash] ASC,
        [ApplicationName] ASC,
        [CreationDate] DESC,
        [DeletionDate] ASC
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Exceptions') AND [name] = 'IX_Exceptions_ApplicationName_DeletionDate_CreationDate_Filtered')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Exceptions_ApplicationName_DeletionDate_CreationDate_Filtered] ON [dbo].[Exceptions] 
    (
        [ApplicationName] ASC,
        [DeletionDate] ASC,
        [CreationDate] DESC
    )
    WHERE DeletionDate IS NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Exceptions') AND [name] = 'IX_Exceptions_CreationDate_Includes')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Exceptions_CreationDate_Includes] ON [dbo].[Exceptions] 
    (
        [CreationDate] ASC
    )
    INCLUDE ([ApplicationName], [MachineName], [DuplicateCount])
END

/* BEGIN V2 Schema changes */

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'LastLogDate')
BEGIN
    ALTER TABLE [dbo].[Exceptions] Add [LastLogDate] [datetime] NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'Category')
BEGIN
    ALTER TABLE [dbo].[Exceptions] Add [Category] nvarchar(100) NULL;
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'SQL')
BEGIN
    ALTER TABLE [dbo].[Exceptions] DROP COLUMN [SQL];
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'SQL')
BEGIN
    ALTER TABLE [dbo].[Exceptions] DROP COLUMN [SQL];
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_ExceptionLevel')
BEGIN
    ALTER TABLE [dbo].[Exceptions] DROP CONSTRAINT [FK_ExceptionLevel];
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ExceptionLevel')
BEGIN
    DROP TABLE dbo.[ExceptionLevel];
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'ExceptionLevelId')
BEGIN
    ALTER TABLE [dbo].[Exceptions] DROP COLUMN [ExceptionLevelId];
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'LogLevel')
BEGIN
    ALTER TABLE [dbo].[Exceptions]
    ADD [LogLevel] tinyint NOT NULL;

    ALTER TABLE dbo.Exceptions
    ADD CONSTRAINT DF_Exceptions_LogLevel
    DEFAULT 5 FOR LogLevel;
END