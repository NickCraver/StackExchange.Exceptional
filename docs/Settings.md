---
layout: default
title: 'Settings'
---
## Settings

Exceptional has many configuration settings you can use to customize logging and additional functionality. The base settings can be configured in `Web.config` ([in the case of ASP.NET (non-core)]({{ site.baseurl }}/AspDotNet)) or via your config JSON or `.UseExceptional()` overloads ([in the case of ASP.NET Core]({{ site.baseurl }}/AspDotNetCore)).

However, **all** settings are always available in code, so pick whatever flavor suits you.

In code, settings are in the `StackExchange.Exceptional` namespace, in [the `Settings` class](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/src/StackExchange.Exceptional.Shared/Settings.cs). The current instance is accessible at `Settings.Current`. For example:

```c#
using StackExchange.Exceptional;
//...
Settings.Current.ApplicationName = "My Application";
```

### Controls

There are a few global controls useful for stopping logging during shutdown events, etc. They are:

**IsLoggingEnabled** (get-only `bool`): Whether logging is currently enabled.

**`DisableLogging()`**: Disables all error logging.

**`EnableLogging()`**: Re-enabled all error logging.

Here are the available settings in shorthand beneath `Settings`, e.g. `Notifiers` is `Settings.Current.Notifiers`.

### Top Level
**ApplicationName** (`string`): This is the name of the application. It's logged with every exception and is used as the unique name in your logging store. For example, 20 apps can all have different application names and share the same SQL database. Tip: [Opserver](https://github.com/opserver/Opserver) can display all of them, this is the dashboard we use to see the entire network at Stack Overflow. Usage:
```c#
ApplicationName = "Project Booya";
```

**DataIncludeRegex** (`Regex`): The pattern of `Exception.Data` keys to automatically include in `CustomData` when logging. For example, `Redis.*|Jil.*` would include all keys that start with `Redis` or `Jil`. sage:
```c#
DataIncludeRegex = new RegEx("Redis.*|Jil.*", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Singleline);
```

**AppendFullStackTraces** (`bool`, default: `true`): Whether to include the full stack trace when an exception is thrown (including the outer-exception stack). Usage:
```c#
AppendFullStackTrace = true;
```

**UseExceptionalPageOnThrow** <span class="badge">ASP.NET Core Only</span> (`bool`, default: `false`): Whether to show the Exceptional page on throw, instead of the built-in `.UseDeveloperExceptionPage()`. This renders the Exceptional error detail page when an error throws in your application (after logging it), useful for local development and a consistent error-viewing experience overall.
```c#
UseExceptionalPageOnThrow = true;
```

**Notifiers** (`List<IErrorNotifier>`): These run just after an exception is logged, like emailing it to a user.
The [`EmailNotifier` is built-in](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/src/StackExchange.Exceptional.Shared/Notifiers/EmailNotifier.cs), but anyone [can implement `IErrorNotifier`](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/src/StackExchange.Exceptional.Shared/Notifiers/IErrorNotifier.cs) for things like posting to a chat room, etc.
Usage: There is a `.Register()` extension method on `IEmailNotifier` to automate usage, e.g. this registers the notifier on `Settings.Current`:
```c#
new EmailNotifier(settings).Register();
```

**ExceptionActions** (`Dictionary<string, Action<Error>>`): These run just before an exception is logged, for each `.InnerException` within that matches. The `string` is the full type name (so that references are less burdensome). [The `.AddDefault()` extensions](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/src/StackExchange.Exceptional.Shared/Extensions.Handlers.cs) are added by default (e.g. for backwards compatability to keep logging SQL exceptions in a rich format). There are extension methods to ease registering here as well: `.AddHandler<T>(Action<Error, T> handler)` and `.AddHandler(string typeName, Action<Error, Exception> handler)`, used like this:
```c#
ExceptionActions.AddHandler<SqlException>((e, se) =>
{
    e.AddCommand(new Command("SQL Server Query", se.Data.Contains("SQL") ? se.Data["SQL"] as string : null)
        .AddData(nameof(se.Server), se.Server)
        .AddData(nameof(se.Number), se.Number.ToString())
        .AddData(nameof(se.LineNumber), se.LineNumber.ToString())
        .AddData(se.Procedure.HasValue(), nameof(se.Procedure), se.Procedure)
    );
});
```

**GetIPAddress** (`Func<string>`): Method of getting the IP address for the error, defaults to retrieving it from server variables, but may need to be replaced in special multi-proxy situations. Usage:
```c#
GetIPAddress = () => SomeMethodThatReturnsAnIp();
```

**GetCustomData** (`Action<Exception, Dictionary<string, string>>`): Method to get custom data for an error; **will only be called when custom data isn't already present**. The `Dictionary` is passed in, just add to it. Usage:
```c#
GetCustomData = (exception, data) =>
{
    data.Add("User Name", UserService.GetUserName());
};
```

### Render

These settings are under `.Render`, e.g. `Settings.Current.Render`.

**JSIncludes** (`List<string>`): A list of a JavaScript files to include to all error log pages, for customizing the behavior and such. Be sure to resolve the path before passing it in here, as it will be rendered literally in the `<script src=""` attribute. Usage:
```c#
Render.JSIncludes.Add("/path/to/my.js");
```

**CSSIncludes** (`List<string>`): Adds a CSS include to all error log pages, for customizing the look and feel.. Be sure to resolve the path before passing it in here, as it will be rendered literally in the `<link href=""` attribute. Usage:
```c#
Render.CSSIncludes.Add("/path/to/my.css");
```


### Store

These settings are under `.Store`, e.g. `Settings.Current.Store`. Usages aren't shown here since these are usually used when creating a store, see the left-hand nav for different store examples.

**Type** (`string`): The type of error store to use, e.g. `Memory`, `SQL`, `MySQL`.

**Path** (`string`): Only for file-based error stores. The path to use on for file storage.

**ConnectionString** (`string`): Only for database-based error stores. The connection string to use.  If provided, `ConnectionStringName` below is ignored.

**ConnectionStringName** <span class="badge">Non-Core Only</span> (`string`): Only for database-based error stores. The name of the connection string to use from the application's configuration.

**Size** (`int`, default: `200`): The size of this error log, either how many to keep (file-based stores) or how many to display (database-based stores).

**RollupPeriod** (`TimeSpan?`, default: `10 Minutes`): The duration of error groups to roll-up, similar errors (those with the same stack trace) within this timespan will be shown as duplicates.

**BackupQueueSize** (`int`, defualt: `1000`): The size of the backup queue to use for the log, after roll-ups, it's how many entries in memory can be stored before culling the oldest.

**BackupQueueRetryInterval** (`TimeSpan`, default: `2 seconds`): When a connection to the error store failed, how often to retry logging the errors in queue for logging. Up to `BackupQueueSize` errors are in the retry queue.


### Ignore (Don't Log)

These settings are under `.Ignore`, e.g. `Settings.Current.Ignore`. These are for completely ignoring errors and not logging them at all.

**Regexes** (`HashSet<Regex>`): Regular expressions collection for errors to ignore. Any errors with a `.ToString()` matching any <see cref="Regex"/> here will not be logged.

**Types** (`HashSet<string>`): Types collection for errors to ignore. Any errors with a Type matching any name here will not be logged.


### LogFilters (Sanitization)

These settings are under `.LogFilters`, e.g. `Settings.Current.LogFilters`. These are for filtering out form and cookie values to prevent logging sensitive data.

**Form** (`Dictionary<string, string>`): Form submitted values to replace on save - this prevents logging passwords, etc. The key is the form value to match, the value is what to replace it with when logging.

**Cookie** (`Dictionary<string, string>`): Cookie values to replace on save - this prevents logging authentication tokens, etc. The key is the cookie name to match, the value is what to use when logging.


### Stack Trace

These settings are under `.StackTrace`, e.g. `Settings.Current.StackTrace`. These are for controlling how stack traces render on the detail pages (and affect nothing at logging time).

**EnablePrettyGenerics** (`bool`, default: `true`): Replaces generic names like ``Dictionary`2`` with `Dictionary<TKey,TValue>`. Specific formatting is based on the `Language` setting below.

**Language** (`CodeLanguage`, default: `CodeLanguage.CSharp`): The language to use when prettifying StackTrace generics. Options are `CodeLanguage.CSharp`, `CodeLanguage.FSharp` and `CodeLanguage.VB`).

**IncludeGenericTypeNames** (`bool`, default: `true`): Whether to print generic type names like `<T1, T2>` etc. or just use commas, e.g. `<,,>` if `Language` is C#.


### Email

These settings are under `.Email`, e.g. `Settings.Current.Email`. These are for email notification, if you'd like some inbox love when an error occurs.

**ToAddress** (`string`): Required to send email. The address to send email messages to.

**FromAddress** (`string`): The address email messages should come from.

**FromDisplayName** (`string`): The display name email messages should come from.

**SMTPHost** (`string`): The SMTP server to send mail through. The app/system configs are the default.

**SMTPPort** (`int?`): The SMTP server port to send mail through. The app/system configs are the default.

**SMTPUserName** (`string`): The SMTP user name to use, if authentication is needed.

**SMTPPassword** (`string`): The SMTP password to use, if authentication is needed.

**SMTPEnableSSL** (`bool`, default: `false`): Whether to use SSL when sending via SMTP.

**PreventDuplicates** (`bool`, default: `false`): Flags whether or not emails are sent for duplicate errors. If `true`, rollup errors (as defined above under `Store` settings) aren't emailed.