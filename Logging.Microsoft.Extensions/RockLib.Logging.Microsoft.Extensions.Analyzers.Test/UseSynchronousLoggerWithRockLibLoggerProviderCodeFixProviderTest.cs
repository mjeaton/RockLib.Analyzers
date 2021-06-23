using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Microsoft.Extensions.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Microsoft.Extensions.Analyzers.UseSynchronousLoggerWithRockLibLoggerProviderAnalyzer,
    RockLib.Logging.Microsoft.Extensions.Analyzers.UseSynchronousLoggerWithRockLibLoggerProviderCodeFixProvider>;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers.Test
{
    [TestClass]
    public class UseSynchronousLoggerWithRockLibLoggerProviderCodeFixProviderTest
    {
        [TestMethod]
        public async Task Foo()
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

        [TestMethod]
        public async Task Bar()
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
