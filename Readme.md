StackExchange.Exceptional is the error handler used internally by Stack Exchange for logging to SQL.

It also supports JSON and memory error stores, filtering of exceptions before logging, and fail/retry mechanisms for storing errors if there's an interruption in connecting to the error store.