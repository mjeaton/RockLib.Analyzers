using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Analyzers.UnexpectedExtendedPropertiesObjectAnalyzer,
    RockLib.Logging.Analyzers.UnexpectedExtendedPropertiesCodeFixProvider>;

namespace RockLib.Logging.Analyzers.Test
{
    public class UnexpectedExtendedPropertiesCodeFixTests
    {
        [Fact(DisplayName = "Code fix applied when extended properties are not provided as an anonymous object")]
        public async Task CodeFixApplied1()
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

        [Fact(DisplayName = "Code fix applied when extended properties are not provided as an anonymous object with object initializer")]
        public async Task CodeFixApplied2()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
using System;
public class Florp
{
    public Florp(string grelp)
    {
        Grelp = grelp;
    }
    public string Grelp { get; }
}

public class Test
{
    public void Call_Log_With_LogEntry_With_Level_Not_Set(ILogger logger)
    {
        [|logger.Info(""no good message"", new Florp(""golem""))|];
    }
}", @"
using RockLib.Logging;
using System;
public class Florp
{
    public Florp(string grelp)
    {
        Grelp = grelp;
    }
    public string Grelp { get; }
}

public class Test
{
    public void Call_Log_With_LogEntry_With_Level_Not_Set(ILogger logger)
    {
        var florp = new Florp(""greninja"");
        logger.Info(""no good message"", new { florp });
    }
}");
        }
    }
}
