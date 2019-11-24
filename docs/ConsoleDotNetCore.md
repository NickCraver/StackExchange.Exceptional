---
layout: default
title: '.NET Console App (non-Core)'
---
## .NET Console Applications (non-Core)

Install [the `StackExchange.Exceptional.AspNetCore` nuget package](https://www.nuget.org/packages/StackExchange.Exceptional.AspNetCore) via:

```powershell
Install-Package StackExchange.Exceptional.AspNetCore
```

**If setting up a console application, I encourage you to [check out the .NET Core Console sample project](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples/Samples.NetCoreConsole), it has all of the below in a proper context.**

You can either configure things via a config file, for example `appsettings.json`:
```json
{
  "Exceptional": {
    "Store": {
      "ApplicationName": "Samples (ASP.NET Core)",
      "Type": "SQL",
      "ConnectionString": "Server=.;Database=Local.Exceptions;Trusted_Connection=True;"
    }
}
```
...and hook up that configuration at startup (this is an example - there are many ways to do this):
```c#
var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var exceptionalSettings = config.GetSection("Exceptional").Get<ExceptionalSettings>();
Exceptional.Configure(exceptionalSettings);
```

Or, you can opt to configure entirely through code instead:

```c#
 Exceptional.Configure(new ExceptionalSettings() { DefaultStore = new SQLErrorStore(_connectionString,"My Application") });
```

...then to log exceptions:

```c#
exception.LogNoContext();
```

#### Optional Configuration

If you want to store some custom key/value style data with an exception, you can use `.AddLogData` extension method, for example:

```c#
exception.AddLogData("Example string", DateTime.UtcNow.ToString())
         .AddLogData("User Id", "You could fetch a user/account Id here, etc.")
         .AddLogData("Links get linkified", "https://www.google.com");
```
...and these pairs will appear on the error detail screen in a "Custom" section and in the `CustomData` dictionary of `Exceptional.Error`.
