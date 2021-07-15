using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.CaughtExceptionShouldBeLoggedAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    public class CaughtExceptionShouldBeLoggedAnalyzerTests
    {
        [Fact(DisplayName = "Diagnostrics are reported when extended property type is not marked as safe to log")]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch(Exception ex)
        {
            logger.Debug(""A debug log without exception"");
        }
    }
}");
        }

    }
}
