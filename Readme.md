StackExchange.Exceptional is the error handler used internally by [Stack Exchange](http://stackexchange.com) and [Stack Overflow](http://stackoverflow.com) for logging to SQL (SQL Server and MySQL are both supported).

It also supports JSON and memory error stores, filtering of exceptions before logging, and fail/retry mechanisms for storing errors if there's an interruption in connecting to the error store.

[See the wiki for how to get configured and logging in just a few minutes](https://github.com/NickCraver/StackExchange.Exceptional/wiki).

While having some features centered around logging/showing exceptions from web applications, **it can be used with either web or console applications**. HttpContext is optional when logging exceptions. 
An example use of this at Stack Exchange is windows services logging to SQL and viewed elsewhere in a central dashboard (I'm working on open sourcing this as well).

About:  
This project was inspired by [ELMAH](http://code.google.com/p/elmah/), but it didn't suit our particular needs for very, very high volume error logging when a network-level event occurs.

Stack Exchange needed a handful things in an error handler/logger:

 - High speed/capacity logging (on the order of 100,000/min)
 - Handling the connection to a central error store being interrupted (without losing the errors)
 - Add custom data to exceptions
 - Rolling up of duplicate errors

Given the above needs, StackExchange.Exceptional was created.  It's as lightweight as possible to suit the needs of the network, but if there are compelling features I'll definitely look at adding them to the main repo here and NuGet soon.

This project is licensed under the [Apache 2.0 license](http://www.apache.org/licenses/LICENSE-2.0).