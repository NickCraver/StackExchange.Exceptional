## StackExchange.Exceptional

StackExchange.Exceptional is the error handler/logger used internally by [Stack Overflow](https://stackoverflow.com) ([Stack Exchange](https://stackexchange.com)) for logging to SQL Server, but many backends (including custom) are supported.
It also supports adding custom data to exceptions as they're logged, filtering of what's logged, ignoring errors, and much more.
[Check out the docs to get started][Docs].

![Build Status](https://ci.appveyor.com/api/projects/status/650qft3qrt2r0gre/branch/master?svg=true)

### Documentation
[See the docs for how to get configured and logging in just a few minutes][Docs].

### Package Status

| Package | NuGet Stable | NuGet Pre-release | Downloads | MyGet |
| ------- | ------------ | ----------------- | --------- | ----- |
| [StackExchange.Exceptional](https://www.nuget.org/packages/StackExchange.Exceptional/) | ![StackExchange.Exceptional](https://img.shields.io/nuget/v/StackExchange.Exceptional.svg) | ![StackExchange.Exceptional](https://img.shields.io/nuget/vpre/StackExchange.Exceptional.svg) | ![StackExchange.Exceptional](https://img.shields.io/nuget/dt/StackExchange.Exceptional.svg) | [![StackExchange.Exceptional MyGet](https://img.shields.io/myget/exceptional/vpre/StackExchange.Exceptional.svg)](https://www.myget.org/feed/exceptional/package/nuget/StackExchange.Exceptional) |
| [StackExchange.Exceptional.AspNetCore](https://www.nuget.org/packages/StackExchange.Exceptional.AspNetCore/) | ![StackExchange.Exceptional.AspNetCore](https://img.shields.io/nuget/v/StackExchange.Exceptional.AspNetCore.svg) | ![StackExchange.Exceptional.AspNetCore](https://img.shields.io/nuget/vpre/StackExchange.Exceptional.AspNetCore.svg) | ![StackExchange.Exceptional.AspNetCore](https://img.shields.io/nuget/dt/StackExchange.Exceptional.AspNetCore.svg) | [![StackExchange.Exceptional.AspNetCore MyGet](https://img.shields.io/myget/exceptional/vpre/StackExchange.Exceptional.AspNetCore.svg)](https://www.myget.org/feed/exceptional/package/nuget/StackExchange.Exceptional.AspNetCore) |
| [StackExchange.Exceptional.MySQL](https://www.nuget.org/packages/StackExchange.Exceptional.MySQL/) | ![StackExchange.Exceptional.MySQL](https://img.shields.io/nuget/v/StackExchange.Exceptional.MySQL.svg) | ![StackExchange.Exceptional.MySQL](https://img.shields.io/nuget/vpre/StackExchange.Exceptional.MySQL.svg) | ![StackExchange.Exceptional.MySQL](https://img.shields.io/nuget/dt/StackExchange.Exceptional.MySQL.svg) | [![StackExchange.Exceptional.MySQL MyGet](https://img.shields.io/myget/exceptional/vpre/StackExchange.Exceptional.MySQL.svg)](https://www.myget.org/feed/exceptional/package/nuget/StackExchange.Exceptional.MySQL) |
| [StackExchange.Exceptional.PostgreSql](https://www.nuget.org/packages/StackExchange.Exceptional.PostgreSql/) | ![StackExchange.Exceptional.PostgreSql](https://img.shields.io/nuget/v/StackExchange.Exceptional.PostgreSql.svg) | ![StackExchange.Exceptional.PostgreSql](https://img.shields.io/nuget/vpre/StackExchange.Exceptional.PostgreSql.svg) | ![StackExchange.Exceptional.PostgreSql](https://img.shields.io/nuget/dt/StackExchange.Exceptional.PostgreSql.svg) | [![StackExchange.Exceptional.PostgreSql MyGet](https://img.shields.io/myget/exceptional/vpre/StackExchange.Exceptional.PostgreSql.svg)](https://www.myget.org/feed/exceptional/package/nuget/StackExchange.Exceptional.PostgreSql) |
| [StackExchange.Exceptional.MongoDB](https://www.nuget.org/packages/StackExchange.Exceptional.MongoDB/) | ![StackExchange.Exceptional.MongoDB](https://img.shields.io/nuget/v/StackExchange.Exceptional.MongoDB.svg) | ![StackExchange.Exceptional.MongoDB](https://img.shields.io/nuget/vpre/StackExchange.Exceptional.MongoDB.svg) | ![StackExchange.Exceptional.MongoDB](https://img.shields.io/nuget/dt/StackExchange.Exceptional.MongoDB.svg) | [![StackExchange.Exceptional.MongoDB MyGet](https://img.shields.io/myget/exceptional/vpre/StackExchange.Exceptional.MongoDB.svg)](https://www.myget.org/feed/exceptional/package/nuget/StackExchange.Exceptional.MongoDB) |
| [StackExchange.Exceptional.Shared](https://www.nuget.org/packages/StackExchange.Exceptional.Shared/) | ![StackExchange.Exceptional.Shared](https://img.shields.io/nuget/v/StackExchange.Exceptional.Shared.svg) | ![StackExchange.Exceptional.Shared](https://img.shields.io/nuget/vpre/StackExchange.Exceptional.Shared.svg) | ![StackExchange.Exceptional.Shared](https://img.shields.io/nuget/dt/StackExchange.Exceptional.Shared.svg) | [![StackExchange.Exceptional.Shared MyGet](https://img.shields.io/myget/exceptional/vpre/StackExchange.Exceptional.Shared.svg)](https://www.myget.org/feed/exceptional/package/nuget/StackExchange.Exceptional.Shared) |

CI Package feeds (created on every build):
- Only StackExchange.Exceptional packages: https://www.myget.org/gallery/exceptional 
- All Stack Overflow packages: https://www.myget.org/gallery/stackoverflow

### License

Dual-licensed under:
 * Apache License, Version 2.0, ([LICENSE-APACHE](LICENSE-APACHE) or https://www.apache.org/licenses/LICENSE-2.0)
 * MIT license ([LICENSE-MIT](LICENSE-MIT) or https://opensource.org/licenses/MIT)

[Docs]: https://nickcraver.com/StackExchange.Exceptional