using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public class UnexpectedExtendedPropertiesCodeFixTests
    {
        [Fact]
        public async Task VerifyWhenExtendedPropertiesAreNotProvidedAsAnonymousObject()
        {
            await TestAssistants.VerifyCodeFixAsync<UnexpectedExtendedPropertiesObjectAnalyzer, UnexpectedExtendedPropertiesCodeFixProvider>(
@"using RockLib.Logging;
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
        var anonymousFlorp = new Florp(""alakazam"");
        [|logger.Info(""no good message"", anonymousFlorp)|];
    }
}", 
@"using RockLib.Logging;
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
        var anonymousFlorp = new Florp(""alakazam"");
        logger.Info(""no good message"", new { anonymousFlorp });
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public async Task VerifyWhenExtendedPropertiesAreNotProvidedAsAnonymousObjectWithInitializer()
        {
            await TestAssistants.VerifyCodeFixAsync<UnexpectedExtendedPropertiesObjectAnalyzer, UnexpectedExtendedPropertiesCodeFixProvider>(
@"using RockLib.Logging;
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
}", 
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public async Task VerifyWhenExtendedPropertiesAreNotProvidedAsAnonymousObjectInLoggingMethod()
        {
            await TestAssistants.VerifyCodeFixAsync<UnexpectedExtendedPropertiesObjectAnalyzer, UnexpectedExtendedPropertiesCodeFixProvider>(
@"using RockLib.Logging;
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
}", 
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }
    }
}
