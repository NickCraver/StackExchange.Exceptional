---
layout: default
title: 'ASP.NET (non-Core)'
---
## ASP.NET (non-Core)

Install [the `StackExchange.Exceptional` nuget package](https://www.nuget.org/packages/StackExchange.Exceptional) via:

```powershell
Install-Package StackExchange.Exceptional
```

**If setting up a web application, I encourage you to [check out the ASP.NET MVC 5 sample project](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples/Samples.MVC5), it has all of the below in a proper context.**

Web.Config example pieces for an IIS 7.5 deployment:

```xml
<configuration>
  <configSections>
    <section name="Exceptional" type="StackExchange.Exceptional.Settings, StackExchange.Exceptional"/>
  </configSections>
  <Exceptional applicationName="Core">
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
  <system.webServer>
    <modules>
      <add name="ErrorStore" type="StackExchange.Exceptional.ExceptionalModule, StackExchange.Exceptional" />
    </modules>
  </system.webServer>
</configuration>
```

This is all optional (except `StackExchange.Exceptional.ExceptionalModule` if you want to capture unhandled MVC exceptions automatically), you can setup completely via code as well.  Examples:

```c#
Exceptional.Configure(settings => settings.DefaultStore = new SQLErrorStore(applicationName: "My Application", connectionString: _connectionString));
```

...then to log exceptions (context would be null for non-web applications):

```c#
exception.Log(_context);
```

#### Optional Configuration

Now for the optional pieces, Stack Overflow exposes the error handler through an MVC route, this allows you to lock it down using whatever security you already have in place:

```c#
[Route("admin/errors/{resource?}/{subResource?}")]
public Task Exceptions() => ExceptionalModule.HandleRequestAsync(System.Web.HttpContext.Current);
```

If you want to customize the views (adding links, etc.) you can add JavaScript files which will be included on both the exception list and exception detail views.  In the exception detail view parsing is not necessary since all of the detail is available via `window.Exception` as well:

```c#
Settings.Current.Render.JSIncludes.Add("/Content/errors.js");
```

If you want to store some custom key/value style data with an exception, you can set up `Settings.GetCustomData`, for example:

```c#
Exceptional.Settings.GetCustomData = (exception, data) =>
    {
        // exception is the exception thrown
        // context is the HttpContext of the request (could be null, e.g. background thread exception)
        // data is a Dictionary<string, string> to add custom data too
        data.Add("Example string", DateTime.UtcNow.ToString());
        data.Add("User Id", "You could fetch a user/account Id here, etc.");
        data.Add("Links get linkified", "https://www.google.com");
    };
```
...and these pairs will appear on the error detail screen in a "Custom" section.

#### Routes

For convenience, there is an easy to add route handler to render your errors. This way you can lock down the route using your current security models. The route iself is simple, in any controller (probably an admin or localhost controller):

```c#
public Task Exceptions() => ExceptionalModule.HandleRequestAsync(System.Web.HttpContext.Current);
```
