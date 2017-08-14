---
layout: default
title: 'ASP.NET Core'
---
## ASP.NET Core

Install [the `StackExchange.Exceptional.AspNetCore` nuget package](https://www.nuget.org/packages/StackExchange.Exceptional.AspNetCore) via:

```powershell
Install-Package StackExchange.Exceptional.AspNetCore
```

**If setting up a web application, I encourage you to [check out the ASP.NET Core sample project](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples/Samples.AspNetCore), it has all of the below in a proper context.**

#### Configuration

You attach Exceptional by adding it in your `Startup.Configure()` method. A few overloads are available:
```c#
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    // This uses all defaults (e.g. the in-memory error store)
    app.UseExceptional(settings => 
    {
        settings.ApplicationName = "My App!";
    });
}
```
...or the other `.UseExceptional()` overloads:
```c#
    // This uses all defaults (e.g. the in-memory error store)
    app.UseExceptional(Configuration.GetSection("Exceptional"));
```
and configure Exceptional in your `Configuration`, e.g. in your `appsettings.json` ([full schema here](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/samples/Samples.AspNetCore/appsettings.json)):
```json
{
  "Exceptional": {
    "ApplicationName": "Samples (ASP.NET Core)"
  },
  "ErrorStore": {
    "Type": "SQL",
    "ConnectionString": "Server=.;Database=Local.Exceptions;Trusted_Connection=True;"
  }
}
```
...or you can use a combination of the two:
```c#
    app.UseExceptional(Configuration.GetSection("Exceptional"), settings => 
    {
        settings.UseExceptionalPageOnThrow = env.IsDevelopment();
    });
```
Note the `UseExceptionalPageOnThrow` property here. This is the Exceptional alternative to `app.UseDeveloperExceptionPage();` to view exceptions as they happen locally in a useful/familiar format.

#### Routes

For convenience, there is an easy to add route handler to render your errors. This way you can lock down the route using your current security models. The route iself is simple, in any controller (probably an admin or localhost controller):

```c#
public async Task Exceptions() => await ExceptionalMiddleware.HandleRequestAsync(HttpContext);
```