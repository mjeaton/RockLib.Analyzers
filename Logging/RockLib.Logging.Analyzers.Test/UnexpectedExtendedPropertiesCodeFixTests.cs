using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Analyzers.UnexpectedExtendedPropertiesObjectAnalyzer,
    RockLib.Logging.Analyzers.UnexpectedExtendedPropertiesCodeFixProvider>;

namespace RockLib.Logging.Analyzers.Test
{
    public class UnexpectedExtendedPropertiesCodeFixTests
    {
        [Fact]
        public async Task Go()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
using System;
public class Florp
{
    public string Grelp { get; set; }
}

public class Test
{
    public void Call_Log_With_LogEntry_With_Level_Not_Set(ILogger logger)
    {
        var anonymousFlorp = new Florp();
        [|logger.Info(""no good message"", anonymousFlorp)|];
    }
}", @"
using RockLib.Logging;
using System;
public class Florp
{
    public string Grelp { get; set; }
}

public class Test
{
    public void Call_Log_With_LogEntry_With_Level_Not_Set(ILogger logger)
    {
        var anonymousFlorp = new Florp();
        logger.Info(""no good message"", new { anonymousFlorp });
    }
}");
        }
    }
}
