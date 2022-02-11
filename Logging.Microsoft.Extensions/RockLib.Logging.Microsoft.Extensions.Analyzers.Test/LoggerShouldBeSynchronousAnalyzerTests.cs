using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers.Test
{
    public static class LoggerShouldBeSynchronousAnalyzerTests
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

        // TODO: Need a test where AddRockLibLoggerProvider() is not called, 
        // and when the processing mode isn't synchronous

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalled()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledWithName()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledAndProviderIsConfiguredElsewhere()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}" + AspNetCoreShim).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledWithNameAndProviderIsConfiguredElsewhere()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}" + AspNetCoreShim).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledWithSynchronousProcessingMode()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledWithNameAndSynchronousProcessingMode()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledWithSynchronousProcessingModeAndProviderIsConfiguredElsewhere()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}" + AspNetCoreShim).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenAddLoggerIsCalledWithSynchronousProcessingModeAddNameAndProviderIsConfiguredElsewhere()
        {
            await TestAssistants.VerifyAnalyzerAsync<LoggerShouldBeSynchronousAnalyzer>(@"
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
}" + AspNetCoreShim).ConfigureAwait(false);
        }
    }
}
