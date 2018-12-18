---
layout: default
title: 'Upgrading to V2'
---
## Upgrading to V2

Exceptional V2 is a major and breaking release in an effort to simplify settings, unify the experience, and add a few missing features from V1. This means that your code likely breaks in a few places, and schemas for storage for SQL providers will need an update.

This doc describes how to upgrade each area of Exceptional you may be using:

#### Logging

Loging has shifted from static methods (`Error.Log()`/`Error.LogWithoutContext()`) to extension methods (`.Log()`/`.LogWithoutContext()`) on `Exception` itself. Example usage in a controller:
```c#
catch (SqlException e)
{
    e.Log(Context);
}
```
Note: `.Log()` is defined separately for ASP.NET (in the `StackExchange.Exceptional` library) and ASP.NET Core (in the `StackExchange.Exceptional.AspNetCore`), due to the `HttpContext` we extract data from being different.

**Category** is a new string field on `Error` (and an optional parameter on `.Log()`) for further subclassifying errors in an application. This could also be set in a handler for example.

#### Controller (ASP.NET non-Core)

A route that forwards to the exceptional module for handling now looks like this:
```c#
[Route("dev/errors/{resource?}/{subResource?}")]
public Task Exceptions() => ExceptionalModule.HandleRequestAsync(System.Web.HttpContext.Current);
```

#### Settings
Settings are now in `.ExceptionalSettings` (defined in the `StackExchange.Exceptional` and `StackExchange.Exceptional.AspNetCore` libraries.

The `StackExchange.Exceptional` (ASP.NET non-Core) package tries to ensure backwards compatability with existing `Web.config` layouts with shims. If something doesn't work, *please* file an issue!

The rest of the settings that were spread across `Error` and `ErrorStore` are now unified in `ExceptionalSettings`. See [the sample applications](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples) for configure-by-code examples in your respective platform. Settings are also avaialble statically if needed at `ExceptionalConfig.Settings`, for example:
```c#
ExceptionalConfig.Settings.Store.ApplicationName = "MyApp";
```

#### SQL Server
1. Run the same script use for initial setup, it's now an upgrade script as well: [SqlServer.sql][SqlServer]

Note: If you are deploying many services that share a store, you can do this in 2 phases by separating out the scripts. Before your first deploy:
```sql
If Not Exists (Select 1 From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'Exceptions' And COLUMN_NAME = 'LastLogDate')
Begin
    Alter Table [dbo].[Exceptions] Add [LastLogDate] [datetime] Null;
End
If Not Exists (Select 1 From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'Exceptions' And COLUMN_NAME = 'Category')
Begin
    Alter Table [dbo].[Exceptions] Add [Category] nvarchar(100) Null;
End
```
To cleanup after the last V1 instance is removed:
```sql
If Exists (Select 1 From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = 'Exceptions' And COLUMN_NAME = 'SQL')
Begin
    Alter Table [dbo].[Exceptions] Drop Column [SQL];
End
```

#### MySQL
1. Run the same script use for initial setup, it's now an upgrade script as well: [MySQL.sql][MySQL]

Note: If you are deploying many services that share a store, you can do this in 2 phases by separating out the scripts. Before your first deploy:
```sql
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
```
To cleanup after the last V1 instance is removed:
```sql
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
```

[SqlServer]: https://github.com/NickCraver/StackExchange.Exceptional/blob/master/DBScripts/SqlServer.sql
[MySQL]: https://github.com/NickCraver/StackExchange.Exceptional/blob/master/DBScripts/MySQL.sql
