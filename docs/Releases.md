---
title: "Release Notes"
layout: "default"
---
### Release Notes
This page tracks major changes included in any update starting with version 2.0.0.

#### Version 2.0.0

##### <span class="critical">Major version breaking changes</span>
  - An upgrade guide for moving from v1 to v2 [can be found here](https://nickcraver.com/StackExchange.Exceptional/UpgradeToV2)
  - Logging has changed fom static methods to `.Log()` and `.LogWithoutContext()` extensions on `Exception`
  - Errors now have a `LastLogDate` which is updated when duplicates are logged
  - Errors now have a `Category` field for use in storage (no UI changes yet)
  - Due to the additions above, new columns are necessary on data stores. Upgrade scripts for every provider above are in [the V2 upgrade guide](https://nickcraver.com/StackExchange.Exceptional/UpgradeToV2).
  - Settings have changed greatly in code and for ASP.NET Core, but existing `web.config` settings should load as-is. The [sample applications](https://github.com/NickCraver/StackExchange.Exceptional/tree/master/samples) and getting started guides above explain usage.

##### **Features**
- ASP.NET Core 2.0+ support ([StackExchange.Exceptional.AspNetCore](https://www.nuget.org/packages/StackExchange.Exceptional.AspNetCore/))
  - [Getting started docs](https://nickcraver.com/StackExchange.Exceptional/AspDotNetCore)
  - Additional `UseExceptionalPageOnThrow` setting, which will use the Exceptional error page when an exception occurs (like [`.UseDeveloperExceptionPage()`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-2.2) but with more detail and any custom logging)
- ASP.NET (non-Core) support ([StackExchange.Exceptional](https://www.nuget.org/packages/StackExchange.Exceptional/))
  - [Getting started docs](https://nickcraver.com/StackExchange.Exceptional/AspDotNet)
  - Existing `web.config` configuration will work, code-based configuration will require changes (see below)
- Non-ASP.NET support for both `net461`+ and `netstandard2.0`+
  - Introduces a `StackExchange.Exceptional.Shared` library (a NuGet dependency) for shared code in all of the above
- Stack traces are color coded and much more readable
  - `async` stack traces are also much less noisy and state machine frames are collapsed
  - SourceLink URLs are supported (GitHub built-in) - if the file source is on GitHub, it will be linked in the HTML stack trace
  - All of the above is usable outside the library via `ExceptionalUtils.StackTrace.HtmlPrettify()`
- `Commands` are generally accessible and not just SQL-centric anymore (for example logging a Redis command)
  - These can be added via `.AddHandler()` ([example here](https://github.com/NickCraver/StackExchange.Exceptional/blob/dbe2b089462554723fe6d45e4f0a6db4cb718937/src/StackExchange.Exceptional.Shared/Extensions.Handlers.cs#L16))
  - Highlighting in the log is provided by [highlight.js](https://highlightjs.org/)
- All methods for storage providers are now `async` (since more are off-box)
  - HttpModule is also `async` now (see getting started guides above for examples)
- Added a `.AddLogData()` extension method on `Exception` for quickly adding key/value pairs for logging custom data
- Supported storage providers
  - In-memory (built-in)
  - JSON on Disk (built-in)
  - SQL Server (built-in)
  - MySQL (via [StackExchange.Exceptional.MySQL](https://www.nuget.org/packages/StackExchange.Exceptional.MySQL/))
  - PostgreSql (via [StackExchange.Exceptional.PostgreSql](https://www.nuget.org/packages/StackExchange.Exceptional.PostgreSql/))
  - MongoDB (via [StackExchange.Exceptional.MongoDB](https://www.nuget.org/packages/StackExchange.Exceptional.MongoDB/))
- Internal changes
  - Moves to `.less` for styling for easier maintenance (currently requires WebCompiler extension for changes)
  - Combined `.js` files via bundler
- For more details, see [the v2 tracking issue #85](https://github.com/NickCraver/StackExchange.Exceptional/issues/85)
