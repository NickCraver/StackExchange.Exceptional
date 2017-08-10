---
layout: default
title: 'SQL Setup'
---

Install for SQL is fairly straightforward, you'll need to do the following:

1. Give the application permissions to a database
2. Run the [SQL to create the Exceptions table](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/DBScripts/SqlServer.sql)
3. Configure the application to use this error store, either [in the web.config](https://github.com/NickCraver/StackExchange.Exceptional/wiki/Setup) or in code.

### web.config example:

```xml
<ErrorStore type="SQL" connectionString="Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors" />
```

### code example:

```c#
StackExchange.Exceptional.ErrorStore.Setup("My Application", new SQLErrorStore("Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors"));
```
