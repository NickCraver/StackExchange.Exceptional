---
layout: default
title: 'Storage: PostgreSql'
---
## Storage: PostgreSql

#### Installation
Install for PostgreSql is as follows:

1. Install [the `StackExchange.Exceptional.PostgreSql` package](https://www.nuget.org/packages/StackExchange.Exceptional.PostgreSql).
2. Run the [SQL to create the Exceptions table][PostgreSql].
3. Configure the application to use this error store.

#### Coniguration
Web.config example:
```xml
<ErrorStore type="PostgreSql" connectionString="Server=..." />
```

ASP.NET Core JSON example:
```json
{
  "Exceptional": {
    "Store": {
      "ApplicationName": "Samples (ASP.NET Core)",
      "Type": "PostgreSql",
      "ConnectionString": "Server=..."
    }
  }
}
```

C# Code example:
```c#
Exceptional.Configure("My Application", new PostgreSqlErrorStore("Server=..."));
```

[PostgreSql]: https://github.com/NickCraver/StackExchange.Exceptional/blob/master/DBScripts/PostgreSql.sql