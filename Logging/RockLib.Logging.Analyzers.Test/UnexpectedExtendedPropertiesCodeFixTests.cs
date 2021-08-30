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
        var anonymousFlorp = new Florp(""abc"");
        [|logger.Info(""no good message"", anonymousFlorp)|];
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
        var anonymousFlorp = new Florp(""abc"");
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
        [|logger.Info(""no good message"", new Florp(""greninja""))|];
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
        logger.Info(""no good message"", new { Florp = new Florp(""greninja"") });
    }
}");
        }

        [Fact(DisplayName = "Code fix applied when extended properties are not provided as an anonymous in Logging method")]
        public async Task CodeFixApplied3()
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
        var logEntry = [|new LogEntry(""no good message"", extendedProperties: new Florp(""frogadier""))|];
        logger.Log(logEntry);
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
        var logEntry = new LogEntry(""no good message"", extendedProperties: new { Florp = new Florp(""frogadier"") });
        logger.Log(logEntry);
    }
}");
        }


    }
}
