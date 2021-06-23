# RockLib0002: Logger should be synchronous

## Cause

This rule fires when a logger is used by a `RockLibLoggerProvider` but does not have a `processingMode` with a value of `Logger.ProcessingMode.Synchronous`.

## Reason for rule

The Microsoft.Extensions.Logging library can provide context that has a finite lifecycle. In order to ensure that that context is not used after its lifecycle has expired, the logger must have a `processingMode` with a value of `Logger.ProcessingMode.Synchronous`.

## How to fix violations

To fix a violation of this rule, apply the appropriate code fix:

- Add 'processingMode' argument with a value of Logger.ProcessingMode.Synchronous
- Change 'processingMode' argument to Logger.ProcessingMode.Synchronous

## Examples

### Violates

`IServiceCollection` extension method:

```c#
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger()
            .AddConsoleLogProvider();

        services.AddRockLibLoggerProvider();
    }
}
```

---

`ILoggingBuilder` extension method:

```c#
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddRockLibLoggerProvider();
            });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger()
            .AddConsoleLogProvider();
    }
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0002 // Logger should be synchronous
#pragma warning restore RockLib0002 // Logger should be synchronous
```
