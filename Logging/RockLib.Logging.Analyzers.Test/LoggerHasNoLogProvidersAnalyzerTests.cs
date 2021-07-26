using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.LoggerHasNoLogProvidersAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    public class LoggerHasNoLogProvidersAnalyzerTests
    {
        [Fact(DisplayName = "Diagnostics are reported when logger builder is non-empty but has no log providers defined")]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

namespace Example.Logging.AspNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Logger builder is non-empty if its option's Level property is set
            [|services.AddLogger(configureOptions: options =>
                {
                    options.Level = LogLevel.Info;
                })|];

            // Logger builder is non-empty if its option's IsDisabled property is set
            [|services.AddLogger(configureOptions: options =>
                {
                    options.IsDisabled = true;
                })|];

            // Logger builder is non-empty if a context provider has been added to it
            [|services.AddLogger()|]
                .AddContextProvider<MyContextProvider>();

            // Logger builder is non-empty if a context provider has been added to it (with local variable)
            var builder = [|services.AddLogger()|];
            builder.AddContextProvider<MyContextProvider>();
        }

        private class MyContextProvider : IContextProvider
        {
            public void AddContext(LogEntry logEntry)
            {
            }
        }
    }
}");
        }

        [Fact(DisplayName = null)]
        public async Task DiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging;
using RockLib.Logging.DependencyInjection;

namespace Example.Logging.AspNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            [|services.AddLogger()|];
        }

        private class MyContextProvider : IContextProvider
        {
            public void AddContext(LogEntry logEntry)
            {
            }
        }
    }
}", ("appsettings.json", @"{
  ""foo"": 123
}"));
        }
    }
}
