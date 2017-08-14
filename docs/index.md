---
layout: default
---
## StackExchange.Exceptional
StackExchange.Exceptional is the error handler used internally by [Stack Exchange](http://stackexchange.com) and [Stack Overflow](http://stackoverflow.com) for logging to SQL (SQL Server and MySQL are both supported).

It also supports JSON and memory error stores, filtering of exceptions before logging, and fail/retry mechanisms for storing errors if there's an interruption in connecting to the error store.

#### Configuration

- [Getting started with ASP.NET (non-Core)]({{ site.baseurl }}/AspDotNet)
- [Getting started with ASP.NET Core]({{ site.baseurl }}/AspDotNetCore)

#### Details
While having some features centered around logging/showing exceptions from web applications, **it can be used with either web or console applications**. `HttpContext` is optional when logging exceptions. 
An example use of this at Stack Exchange is windows services logging to SQL and viewed elsewhere in a central dashboard called [Opserver](https://github.com/opserver/Opserver).

This project was inspired by [ELMAH](https://code.google.com/p/elmah/), but it didn't suit our particular needs for very, very high volume error logging when a network-level event occurs.

Stack Exchange needed a handful things in an error handler/logger:

 - High speed/capacity logging (on the order of 100,000/min)
 - Handling the connection to a central error store being interrupted (without losing the errors)
 - Add custom data to exceptions
 - Rolling up of duplicate errors

Given the above needs, StackExchange.Exceptional was created.  It's as lightweight as possible to suit the needs of the network, but if there are compelling features I'll definitely look at adding them to the main repo here and NuGet soon.

#### License

Dual-licensed under:
 * Apache License, Version 2.0, ([LICENSE-APACHE](LICENSE-APACHE) or https://www.apache.org/licenses/LICENSE-2.0)
 * MIT license ([LICENSE-MIT](LICENSE-MIT) or https://opensource.org/licenses/MIT)
