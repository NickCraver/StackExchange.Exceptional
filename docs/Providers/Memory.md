---
layout: default
title: 'Storage: Memory'
---
## Storage: Memory

The memory store is by nature volatile and app-domain specific. This means it's not ideal for a web farm or services that restart often since viewing them will be more of a burden. A centralized store like SQL Server (or anything!) is recommended for these cases.

#### Installation
No install required, it's built-in and is the default store.

#### Coniguration
Web.config example:
```xml
<ErrorStore type="Memory" />
```

ASP.NET Core JSON example:
```json
{
  "Exceptional": {
    "Store": {
      "ApplicationName": "Samples (ASP.NET Core)",
       "Type": "Memory",
       "Size": 500
    }
  }
}
```

C# Code example:
```c#
// Do nothing! It's the default.
```