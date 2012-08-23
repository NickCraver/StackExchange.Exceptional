StackExchange.Exceptional is the error handler used internally by [Stack Exchange](http://stackexchange.com) and [Stack Overflow](http://stackoverflow.com) for logging to SQL.

It also supports JSON and memory error stores, filtering of exceptions before logging, and fail/retry mechanisms for storing errors if there's an interruption in connecting to the error store.

This project is licensed under the [Apache 2.0 license](http://www.apache.org/licenses/LICENSE-2.0).