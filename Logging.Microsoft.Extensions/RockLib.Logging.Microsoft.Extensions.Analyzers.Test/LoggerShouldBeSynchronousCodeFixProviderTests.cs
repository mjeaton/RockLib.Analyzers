using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Microsoft.Extensions.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Microsoft.Extensions.Analyzers.LoggerShouldBeSynchronousAnalyzer,
    RockLib.Logging.Microsoft.Extensions.Analyzers.LoggerShouldBeSynchronousCodeFixProvider>;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers.Test
{
    [TestClass]
    public class LoggerShouldBeSynchronousCodeFixProviderTests
    {
        [TestMethod(null)]
        public async Task CodeFixApplied1()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
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
}", @"
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
        public async Task CodeFixApplied2()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.[|AddLogger(processingMode: Logger.ProcessingMode.Background)|]
            .AddConsoleLogProvider();

        services.AddRockLibLoggerProvider();
    }
}", @"
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
    }
}
