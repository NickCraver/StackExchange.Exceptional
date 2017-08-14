---
layout: default
title: 'Storage: JSON'
---
## Storage: JSON

The JSON error store is file-based and not the most performant for high throughput situations (due to the performance of file systems in general). It's just fine for smaller applications though.

#### Installation
No install required, it's built-in.

#### Coniguration
Web.config example:
```xml
<ErrorStore type="JSON" path="~/Errors" size="200" />
```

ASP.NET Core JSON example:
```json
{
  "Exceptional": {
    "ApplicationName": "Samples (ASP.NET Core)",
      "ErrorStore": {
        "Type": "JSON",
        "Path": "/Errors",
        "Size": 200
     }
  }
}
```

C# Code example:
```c#
ErrorStore.Setup("My Application", new JSONErrorStore(path: "Errors", size: 200));
```