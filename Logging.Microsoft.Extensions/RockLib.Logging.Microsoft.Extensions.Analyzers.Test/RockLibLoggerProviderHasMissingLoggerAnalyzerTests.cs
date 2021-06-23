using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Microsoft.Extensions.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Microsoft.Extensions.Analyzers.RockLibLoggerProviderHasMissingLoggerAnalyzer>;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers.Test
{
    [TestClass]
    public class RockLibLoggerProviderHasMissingLoggerAnalyzerTests
    {
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
        services.AddLogger(""MyLogger"");
        services.[|AddRockLibLoggerProvider()|];
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
        services.AddLogger();
        services.[|AddRockLibLoggerProvider(""MyLogger"")|];
    }
}");
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
        services.AddLogger();
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
        services.AddLogger(""MyLogger"");
        services.AddRockLibLoggerProvider(""MyLogger"");
    }
}");
        }
    }
}
