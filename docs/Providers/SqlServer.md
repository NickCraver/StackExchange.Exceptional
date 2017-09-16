---
layout: default
title: 'Storage: SQL Server'
---
## Storage: SQL Server

Install for SQL Server is fairly straightforward, you'll need to do the following:

1. Give the application permissions to a database.
2. Run the [SQL to create the Exceptions table][SqlServer].
3. Configure the application to use this error store, either [in the web.config](https://github.com/NickCraver/StackExchange.Exceptional/wiki/Setup) or in code.

#### Coniguration
Web.config example:
```xml
<ErrorStore type="SQL" connectionString="Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors" />
```

ASP.NET Core JSON example:
```json
{
  "Exceptional": {
    "Store": {
      "ApplicationName": "Samples (ASP.NET Core)",
      "Type": "SQL",
      "ConnectionString": "Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors"
    }
  }
}
```

C# Code example:
```c#
Exceptional.Configure("My Application", new SQLErrorStore("Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors"));
```

[SqlServer]: https://github.com/NickCraver/StackExchange.Exceptional/blob/master/DBScripts/SqlServer.sql