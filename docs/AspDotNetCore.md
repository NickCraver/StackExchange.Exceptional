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

You register Exceptional by adding it in your `Startup.ConfigureServices()` method. A few overloads are available:

```c#
public void ConfigureServices(IServiceCollection services)
{
    // This uses all defaults (e.g. the in-memory error store)
    services.AddExceptional(settings =>
    {
        settings.ApplicationName = "Samples.AspNetCore";
    });
}
```

...or the other `.AddExceptional()` overloads:
```c#
    // This uses all defaults (e.g. the in-memory error store)
    services.AddExceptional(Configuration.GetSection("Exceptional"));
```
and configure Exceptional in your `Configuration`, e.g. in your `appsettings.json` ([full schema here](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/samples/Samples.AspNetCore/appsettings.json)):
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
...or you can use a combination of the two:
```c#
    services.AddExceptional(Configuration.GetSection("Exceptional"), settings =>
    {
        settings.UseExceptionalPageOnThrow = HostingEnvironment.IsDevelopment();
    });
```
Note the `UseExceptionalPageOnThrow` property here. This is the Exceptional alternative to `app.UseDeveloperExceptionPage();` to view exceptions as they happen locally in a useful/familiar format.

#### Middleware

To add the Exceptional middleware for handling errors, add it to your `Startup.Configure()` method:
```c#
public void Configure(IApplicationBuilder app)
{
    app.UseExceptional();
}
```

Note that you should call this before anything you want handled, as exceptions will "bubble up" to this point. For example, you almost certainly want this called **before** `app.UseMvc();`, so that any errors MVC (or your code running inside it) throws are handled.

#### Routes

For convenience, there is an easy to add route handler to render your errors. This way you can lock down the route using your current security models. The route iself is simple, in any controller (probably an admin or localhost controller):

```c#
public async Task Exceptions() => await ExceptionalMiddleware.HandleRequestAsync(HttpContext);
```
