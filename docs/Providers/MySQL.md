---
layout: default
title: 'Storage: MySQL'
---
## Storage: MySQL

#### Installation
Install for MySQL is as follows:

1. Install [the `StackExchange.Exceptional.MySQL` package](https://www.nuget.org/packages/StackExchange.Exceptional.MySQL).
2. Run the [SQL to create the Exceptions table][MySQL].
3. Configure the application to use this error store.

#### Coniguration
Web.config example:
```xml
<ErrorStore type="MySQL" connectionString="Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors" />
```

ASP.NET Core JSON example:
```json
{
  "Exceptional": {
    "Store": {
      "ApplicationName": "Samples (ASP.NET Core)",
      "Type": "MySQL",
      "ConnectionString": "Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors"
    }
  }
}
```

C# Code example:
```c#
Exceptional.Configure("My Application", new MySQLErrorStore("Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors"));
```

[MySQL]: https://github.com/NickCraver/StackExchange.Exceptional/blob/master/DBScripts/MySQL.sql