using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Microsoft.Extensions.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Microsoft.Extensions.Analyzers.LoggerShouldBeSynchronousAnalyzer>;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers.Test
{
    [TestClass]
    public class LoggerShouldBeSynchronousAnalyzerTests
    {
        private const string AspNetCoreShim = @"

namespace Microsoft.Extensions.Hosting
{
    using System;

    public static class GenericHostBuilderExtensions
    {
        public static IHostBuilder ConfigureWebHostDefaults(this IHostBuilder builder, Action<IWebHostBuilder> configure)
        {
            return builder;
        }
    }
}

namespace Microsoft.AspNetCore.Hosting
{
    public interface IWebHostBuilder
    {
    }

    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseStartup<TStartup>(this IWebHostBuilder hostBuilder) where TStartup : class
        {
            return hostBuilder;
        }
    }
}";

        [TestMethod(null)]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.[|AddLogger()|]
            .AddConsoleLogProvider();

        services.AddRockLibLoggerProvider();
    }
}");
        }

        [TestMethod(null)]
        public async Task DiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.[|AddLogger(""MyLogger"")|]
            .AddConsoleLogProvider();

        services.AddRockLibLoggerProvider(""MyLogger"");
    }
}");
        }

        [TestMethod(null)]
        public async Task DiagnosticsReported3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
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
        services.[|AddLogger()|]
            .AddConsoleLogProvider();
    }
}" + AspNetCoreShim);
        }

        [TestMethod(null)]
        public async Task DiagnosticsReported4()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
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
                logging.AddRockLibLoggerProvider(""MyLogger"");
            });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.[|AddLogger(""MyLogger"")|]
            .AddConsoleLogProvider();
    }
}" + AspNetCoreShim);
        }

        [TestMethod(null)]
        public async Task NoDiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger(processingMode: Logger.ProcessingMode.Synchronous)
            .AddConsoleLogProvider();

        services.AddRockLibLoggerProvider();
    }
}");
        }

        [TestMethod(null)]
        public async Task NoDiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger(""MyLogger"", processingMode: Logger.ProcessingMode.Synchronous)
            .AddConsoleLogProvider();

        services.AddRockLibLoggerProvider(""MyLogger"");
    }
}");
        }

        [TestMethod(null)]
        public async Task NoDiagnosticsReported3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
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
        services.AddLogger(processingMode: Logger.ProcessingMode.Synchronous)
            .AddConsoleLogProvider();
    }
}" + AspNetCoreShim);
        }

        [TestMethod(null)]
        public async Task NoDiagnosticsReported4()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
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
                logging.AddRockLibLoggerProvider(""MyLogger"");
            });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger(""MyLogger"", processingMode: Logger.ProcessingMode.Synchronous)
            .AddConsoleLogProvider();
    }
}" + AspNetCoreShim);
        }
    }
}
