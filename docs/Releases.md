---
title: "Release Notes"
layout: "default"
---
### Release Notes
This page tracks major changes included in any update starting with version 2.0.0.

#### Version 2.0.0
- ASP.NET Core 2.0+ support ([StackExchange.Exceptional.AspNetCore](https://www.nuget.org/packages/StackExchange.Exceptional.AspNetCore/))
  - [Getting started docs](https://nickcraver.com/StackExchange.Exceptional/AspDotNetCore)
  - Additional `UseExceptionalPageOnThrow` setting, which will use the Exceptional error page when an exception occurs (like [`.UseDeveloperExceptionPage()`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-2.2) but with more detail and any custom logging).
- ASP.NET (non-Core) support ([StackExchange.Exceptional](https://www.nuget.org/packages/StackExchange.Exceptional/))
  - [Getting started docs](https://nickcraver.com/StackExchange.Exceptional/AspDotNet)
  - Existing `web.config` configuration will work, code-based configuration will require changes (see below)
- Supported storage providers
  - In-memory (built-in)
  - JSON on Disk (built-in)
  - SQL Server (built-in)
  - MySQL (via [StackExchange.Exceptional.MySQL](https://www.nuget.org/packages/StackExchange.Exceptional.MySQL/))
  - PostgreSql (via [StackExchange.Exceptional.PostgreSql](https://www.nuget.org/packages/StackExchange.Exceptional.PostgreSql/))
  - MongoDB (via [StackExchange.Exceptional.MongoDB](https://www.nuget.org/packages/StackExchange.Exceptional.MongoDB/))
- **Major version breaking changes**
  - An upgrade guide for moving from v1 to v2 [can be found here](https://nickcraver.com/StackExchange.Exceptional/UpgradeToV2)
  - Logging has changed fom static methods to `.Log()` and `.LogWithoutContext()` extensions on `Exception`
  - Errors now have a `LastLogDate` which is updated when duplicates are logged
  - Errors now have a `Category` field for use in storage (no UI changes yet)
  - Settings have changed greatly in code and for ASP.NET Core, but existing `web.config` settings should load as-is. The [sample applications](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples) and getting started guides above explain usage.
  - Due to the additions above, new columns are necessary on data stores. Upgrade scripts for every provider above are in [the V2 upgrade guide](https://nickcraver.com/StackExchange.Exceptional/UpgradeToV2).