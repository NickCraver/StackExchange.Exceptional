CREATE TABLE [dbo].[Exceptions](
	[Id] [bigint] NOT NULL IDENTITY,
	[GUID] [uniqueidentifier] NOT NULL,
	[ApplicationName] [nvarchar](50) NOT NULL,
	[MachineName] [nvarchar](50) NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[Type] [nvarchar](100) NOT NULL,
	[IsProtected] [bit] NOT NULL default(0),
	[Host] [nvarchar](100) NULL,
	[Url] [nvarchar](500) NULL,
	[HTTPMethod] [nvarchar](10) NULL,
	[IPAddress] [varchar](40) NULL,
	[Source] [nvarchar](100) NULL,
	[Message] [nvarchar](1000) NULL,
	[Detail] [nvarchar](max) NULL,	
	[StatusCode] [int] NULL,
	[SQL] [nvarchar](max) NULL,
	[DeletionDate] [datetime] NULL,
	[FullJson] [nvarchar](max) NULL,
	[ErrorHash] [int] NULL,
	[DuplicateCount] [int] NOT NULL default(1)
 CONSTRAINT [PK_Exceptions] PRIMARY KEY CLUSTERED ([Id] ASC)
 WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_Exceptions_GUID_ApplicationName_DeletionDate_CreationDate] ON [dbo].[Exceptions] 
(
	[GUID] ASC,
	[ApplicationName] ASC,
	[DeletionDate] ASC,
	[CreationDate] DESC
)

CREATE NONCLUSTERED INDEX [IX_Exceptions_ErrorHash_ApplicationName_CreationDate_DeletionDate] ON [dbo].[Exceptions] 
(
	[ErrorHash] ASC,
	[ApplicationName] ASC,
	[CreationDate] DESC,
	[DeletionDate] ASC
)

CREATE NONCLUSTERED INDEX [IX_Exceptions_ApplicationName_DeletionDate_CreationDate_Filtered] ON [dbo].[Exceptions] 
(
	[ApplicationName] ASC,
	[DeletionDate] ASC,
	[CreationDate] DESC
)
WHERE DeletionDate Is Null