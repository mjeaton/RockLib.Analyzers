# RockLib0003: RockLibLoggerProvider has missing logger

## Cause

This rule fires when a `RockLibLoggerProvider` is added to an `IServiceCollection` or `ILoggingBuilder`, but a logger with a matching name is *not* registered with the service collection.

## Reason for rule

`RockLibLoggerProvider` has a dependency on a named instance of `RockLib.Logging.ILogger`. If such a logger is not registered with the service collection, the `RockLibLoggerProvider` will throw an exception when initialized.

## How to fix violations

Add a logger to the service collection that has a matching name.

## Examples

### Violates

Default logger name:

```c#
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRockLibLoggerProvider();
    }
}
```

---

Logger name specified:

```c#
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRockLibLoggerProvider("MyLogger");
    }
}
```

### Does not violate

Default logger name:

```c#
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger();
        services.AddRockLibLoggerProvider();
    }
}
```

---

Logger name specified:

```c#
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger("MyLogger");
        services.AddRockLibLoggerProvider("MyLogger");
    }
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0003 // RockLibLoggerProvider has missing logger
#pragma warning restore RockLib0003 // RockLibLoggerProvider has missing logger
```
