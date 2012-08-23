StackExchange.Exceptional is the error handler used internally by [Stack Exchange](http://stackexchange.com) and [Stack Overflow](http://stackoverflow.com) for logging to SQL.

It also supports JSON and memory error stores, filtering of exceptions before logging, and fail/retry mechanisms for storing errors if there's an interruption in connecting to the error store.

About:  
This project was insired by [ELMAH](http://code.google.com/p/elmah/), but it didn't suit our particular needs for very, very high volume error logging when a network-level event occurs.

Stack Exchange needed a handful things in an error handler/logger:

 - High speed/capacity logging (on the order of 100,000/min)
 - Handling the connection to a central error store being interrupted (without losing the errors)
 - Add custom data to exceptions
 - Rolling up of duplicate errors

Given the above needs, StackExchange.Exceptional was created.  It's as lightweight as possible to suit the needs of the network, but if there are compelling features I'll definitely look at adding them to the main repo here and NuGet soon.

This project is licensed under the [Apache 2.0 license](http://www.apache.org/licenses/LICENSE-2.0).