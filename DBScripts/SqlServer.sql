/* 
    SQL Server setup script for Exceptional
    Run this script for creating the exceptions table
    It will also upgrade a V1 schema to V2, just run the full script.
*/
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE [TABLE_SCHEMA] = 'dbo' AND [TABLE_NAME] = 'ExceptionLevel')
BEGIN
    CREATE TABLE [dbo].[ExceptionLevel] (
        [Id] int NOT NULL IDENTITY (1,1),
        [Name] varchar(8) NOT NULL
        CONSTRAINT PK_ExceptionLevel PRIMARY KEY CLUSTERED (Id)
    );

    SET IDENTITY_INSERT ExceptionLevel ON;

    INSERT INTO ExceptionLevel (Id, Name)
    VALUES (1, 'Trace'),
           (2, 'Debug'),
           (3, 'Info'),
           (4, 'Warning'),
           (5, 'Error'),
           (6, 'Critical');

    SET IDENTITY_INSERT ExceptionLevel OFF;
END
GO

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
        [ExceptionLevelId] int NOT NULL DEFAULT(6) CONSTRAINT [FK_ExceptionLevel] FOREIGN KEY REFERENCES dbo.ExceptionLevel(Id)
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

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Exceptions' AND COLUMN_NAME = 'ExceptionLevelId')
BEGIN
    ALTER TABLE [dbo].[Exceptions] 
    ADD [ExceptionLevelId] int NOT NULL DEFAULT(6);

    ALTER TABLE [dbo].[Exceptions]  WITH CHECK
    ADD CONSTRAINT [FK_ExceptionLevel] FOREIGN KEY([ExceptionLevelId])
    REFERENCES [dbo].[ExceptionLevel] ([Id]);
END