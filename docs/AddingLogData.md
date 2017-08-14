---
layout: default
title: 'Adding Log Data'
---
## Adding Log Data

An exception logger wouldn't be very awesome if you couldn't add some data when you log an exception. Exceptional allows you to do this in several ways.

**Extension** `.AddLogData<T>(string key, string value)`: This extension allows adding of key/value pairs when logging an exception. This is useful for logging which server was being hit, what the count in a queue was, etc. Basically any infomation you may think is relevant for debugging. The simplest usage is:
```c#
new Exception("Oops.")
    .AddLogData("MyKey", "MyValue")
    .Log();
```

**Setting** `Settings.GetCustomData`: This hook lets you add custom data (*if it's not already provided*) to an exception while logging. Example usage:
```c#
Settings.Current.GetCustomData = (exception, data) =>
{
    data.Add("Example string", DateTime.UtcNow.ToString());
    data.Add("User Id", "You could fetch a user/account Id here, etc.");
    data.Add("Links get linkified", "https://www.google.com");
};
```

**Extension/Setting** `.AddHandler<T>(Action<Error, T>)` (and `.AddHandler(string, Action<Error, Exception>)`): Any handlers added execute for each exception (including inner exceptions) that are thrown. It's very useful for having a single place to handle adding additional data based on the command type. Any custom system you are using can add data here.

**Interface** `IExceptionalHandled`: This is an interface your custom exception types can implement. If it does implement it, it's called upon logging (again, even if it's an inner exception). This approach let's you keep all the logging of additional useful data for an exception with the definition. Here's an example:
```c#
public class RedisException : Exception, IExceptionalHandled
{
    public RedisException(string message) : base(message) { }

    public void ExceptionalHandler(Error e)
    {
        var cmd = e.AddCommand(new Command("Redis"));
        foreach (string k in e.Exception.Data.Keys) // e.Exception == this
        {
            var val = e.Exception.Data[k] as string;
            if (k == "redis-command") cmd.CommandString = val;
            if (k.StartsWith("Redis-")) cmd.AddData(k.Substring("Redis-".Length), val);
        }
    }
}
```

#### Commands

Commands are a V2 feature and replace the V1 SQL-only logging field. Commands have a type (a title/description), a command string (e.g. the SQL query), and a key/value store for any relevant data you want to associate there, e.g. the SQL Server it was hitting, the timeout, etc.

Here's an example of how a command is added in a handler (this example is one of [the default handlers](https://github.com/NickCraver/StackExchange.Exceptional/blob/master/src/StackExchange.Exceptional.Shared/Extensions.Handlers.cs)):

```c#
Handlers.AddHandler<SqlException>((e, se) =>
{
    if (se.Data == null) return;
    e.AddCommand(new Command("SQL Server Query", se.Data.Contains("SQL") ? se.Data["SQL"] as string : null)
        .AddData(nameof(se.Server), se.Server)
        .AddData(nameof(se.Number), se.Number.ToString())
        .AddData(nameof(se.LineNumber), se.LineNumber.ToString())
        .AddData(se.Procedure.HasValue(), nameof(se.Procedure), se.Procedure)
    );
});
```

Each command is rendered as a section in the detail view, like this:
![Error Command]({{ site.baseurl }}/images/Command.png)