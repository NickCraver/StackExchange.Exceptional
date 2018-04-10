---
layout: default
title: '.NET Console App (non-Core)'
---
## .NET Console Applications (non-Core)

Install [the `StackExchange.Exceptional` nuget package](https://www.nuget.org/packages/StackExchange.Exceptional) via:

```powershell
Install-Package StackExchange.Exceptional
```

**If setting up a console application, I encourage you to [check out the Console sample project](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples/Samples.Console), it has all of the below in a proper context.**


App.Config example pieces:

```xml
<configuration>
  <configSections>
    <section name="Exceptional" type="StackExchange.Exceptional.ConfigSettings, StackExchange.Exceptional"/>
  </configSections>
  <Exceptional applicationName="Samples.Console">
    <IgnoreErrors>
        <!-- Error messages to ignore (optional) -->
        <Regexes>
            <add name="connection suuuuuuuucks" pattern="Request timed out\.$" />
        </Regexes>
        <!-- Error types to ignore, e.g. <add type="System.Exception" /> or -->
        <Types>
          <add type="MyNameSpace.MyException" />
        </Types>
    </IgnoreErrors>
    <!-- Error log store to use -->
    <ErrorStore type="Memory" />
    <!--<ErrorStore type="JSON" path="~\Errors" size="200" rollupSeconds="300" />-->
    <!--<ErrorStore type="SQL" connectionString="Data Source=.;Initial Catalog=Exceptions;Uid=Exceptions;Pwd=iloveerrors" />-->
  </Exceptional>
</configuration>
```

This is all optional, you can setup completely via code as well.  Examples:

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
