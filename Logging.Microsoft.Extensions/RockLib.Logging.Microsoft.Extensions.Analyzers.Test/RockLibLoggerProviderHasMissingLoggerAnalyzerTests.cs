using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers.Test
{
    public static class RockLibLoggerProviderHasMissingLoggerAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenNameIsNotGivenToProvider()
        {
            await TestAssistants.VerifyAnalyzerAsync<RockLibLoggerProviderHasMissingLoggerAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger(""MyLogger"");
        services.[|AddRockLibLoggerProvider()|];
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenNameIsNotGivenToLogger()
        {
            await TestAssistants.VerifyAnalyzerAsync<RockLibLoggerProviderHasMissingLoggerAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger();
        services.[|AddRockLibLoggerProvider(""MyLogger"")|];
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenNamesAreNotGiven()
        {
            await TestAssistants.VerifyAnalyzerAsync<RockLibLoggerProviderHasMissingLoggerAnalyzer>(@"
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenNamesMatch()
        {
            await TestAssistants.VerifyAnalyzerAsync<RockLibLoggerProviderHasMissingLoggerAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogger(""MyLogger"");
        services.AddRockLibLoggerProvider(""MyLogger"");
    }
}").ConfigureAwait(false);
        }
    }
}
