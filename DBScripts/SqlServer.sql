/* 
    SQL Server setup script for Exceptional
    Run this script for creating the exceptions table
    It will also upgrade a V1 schema to V2, just run the full script.
*/
If Not Exists (Select 1 
                 From INFORMATION_SCHEMA.TABLES 
                Where [TABLE_SCHEMA] = 'dbo'
                  And [TABLE_NAME] = 'Exceptions')
Begin
    Create Table [dbo].[Exceptions](
        [Id] [bigint] Not Null Identity,
        [GUID] [uniqueidentifier] Not Null,
        [ApplicationName] [nvarchar](50) Not Null,
        [MachineName] [nvarchar](50) Not Null,
        [CreationDate] [datetime] Not Null,
        [Type] [nvarchar](100) Not Null,
        [IsProtected] [bit] Not Null Default(0),
        [Host] [nvarchar](100) Null,
        [Url] [nvarchar](500) Null,
        [HTTPMethod] [nvarchar](10) Null,
        [IPAddress] [varchar](40) Null,
        [Source] [nvarchar](100) Null,
        [Message] [nvarchar](1000) Null,
        [Detail] [nvarchar](max) Null,	
        [StatusCode] [int] Null,
        [DeletionDate] [datetime] Null,
        [FullJson] [nvarchar](max) Null,
        [ErrorHash] [int] Null,
        [DuplicateCount] [int] Not Null Default(1),
        [LastLogDate] [datetime] Null,
        [Category] nvarchar(100) Null
     Constraint [PK_Exceptions] Primary Key Clustered ([Id] Asc)
     With (Pad_Index = Off, Statistics_NoRecompute = Off, Ignore_Dup_Key = Off, Allow_Row_Locks = On, Allow_Page_Locks = On) On [PRIMARY]
    );
End

If Not Exists (Select 1 From sys.indexes Where object_id = OBJECT_ID('dbo.Exceptions') And name = 'IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate')
Begin
    Create Unique Nonclustered Index [IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate] On [dbo].[Exceptions] 
    (
        [GUID] Asc,
        [ApplicationName] Asc,
        [DeletionDate] Asc,
        [CreationDate] Desc
    );
End

If Not Exists (Select 1 From sys.indexes Where object_id = OBJECT_ID('dbo.Exceptions') And name = 'IX_Exceptions_ErrorHash_ApplicationName_CreationDate_DeletionDate')
Begin
    Create Nonclustered Index [IX_Exceptions_ErrorHash_ApplicationName_CreationDate_DeletionDate] On [dbo].[Exceptions] 
    (
        [ErrorHash] Asc,
        [ApplicationName] Asc,
        [CreationDate] Desc,
        [DeletionDate] Asc
    );
End

If Not Exists (Select 1 From sys.indexes Where object_id = OBJECT_ID('dbo.Exceptions') And name = 'IX_Exceptions_ApplicationName_DeletionDate_CreationDate_Filtered')
Begin
    Create Nonclustered Index [IX_Exceptions_ApplicationName_DeletionDate_CreationDate_Filtered] On [dbo].[Exceptions] 
    (
        [ApplicationName] Asc,
        [DeletionDate] Asc,
        [CreationDate] Desc
    )
    Where DeletionDate Is Null;
End

/* Begin V2 Schema changes */

If Not Exists (Select 1 From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'Exceptions' And COLUMN_NAME = 'LastLogDate')
Begin
    Alter Table [dbo].[Exceptions] Add [LastLogDate] [datetime] Null;
End

If Not Exists (Select 1 From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'Exceptions' And COLUMN_NAME = 'Category')
Begin
    Alter Table [dbo].[Exceptions] Add [Category] nvarchar(100) Null;
End

If Exists (Select 1 From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'Exceptions' And COLUMN_NAME = 'SQL')
Begin
    Alter Table [dbo].[Exceptions] Drop Column [SQL];
End