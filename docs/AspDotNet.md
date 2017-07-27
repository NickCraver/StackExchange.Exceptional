---
layout: default
title: 'ASP.NET Setup'
---

Install the nuget package via:

```ps
Install-Package StackExchange.Exceptional
```

**If setting up a web application, I encourage you to [check out the MVC sample project](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples/Samples.MVC5), it has all of the below in a proper context.**

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
StackExchange.Exceptional.ErrorStore.Setup("My Application", new SQLErrorStore(_connectionString));
```

...then to log exceptions (context would be null for non-web applications):

```c#
ErrorStore.LogException(exception, _context);
```

Now for the optional pieces, Stack Overflow exposes the error handler through an MVC route, this allows you to lock it down using whatever security you already have in place:

```c#
[Route("admin/errors/{resource?}/{subResource?}")]
public ActionResult InvokeErrorHandler(string resource, string subResource)
{
    var context = System.Web.HttpContext.Current;
    var factory = new StackExchange.Exceptional.HandlerFactory();

    var page = factory.GetHandler(context, Request.RequestType, Request.Url.ToString(), Request.PathInfo);
    page.ProcessRequest(context);

    return null;
}
```

If you want to customize the views (adding links, etc.) you can add JavaScript files which will be included on both the exception list and exception detail views.  In the exception detail view parsing is not necessary since all of the detail is available via `window.Exception` as well:

```c#
ErrorStore.AddJSInclude("~/content/js/errors.js");
```

If you want to store some custom key/value style data with an exception, you can set up `ErrorStore.GetCustomData`, for example:

```c#
ErrorStore.GetCustomData = GetCustomErrorData;
```

Defined as:

```c#
private static void GetCustomErrorData(Exception ex, HttpContext context, Dictionary<string, string> data)
{
    data.Add("Key","Value");
}
```

...these pairs will appear on the error detail screen in a "Custom" section.