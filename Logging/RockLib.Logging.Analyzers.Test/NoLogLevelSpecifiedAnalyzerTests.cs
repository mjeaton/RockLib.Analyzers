using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class NoLogLevelSpecifiedAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenLevelIsNotSet()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogLevelSpecifiedAnalyzer>(
@"using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Level_Not_Set(ILogger logger)
    {
        logger.Log([|new LogEntry()|]);

        logger.Log([|new LogEntry(""Hello, world!"")|]);

        LogEntry logEntry1 = new LogEntry();
        logger.Log([|logEntry1|]);

        LogEntry logEntry2 = new LogEntry(""Hello, world!"");
        logger.Log([|logEntry2|]);
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLevelIsSet()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogLevelSpecifiedAnalyzer>(
@"using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Level_Set(ILogger logger)
    {
        logger.Log(new LogEntry(""Hello, world!"", LogLevel.Error));

        logger.Log(new LogEntry { Level = LogLevel.Error });

        LogEntry logEntry1 = new LogEntry(""Hello, world!"", LogLevel.Error);
        logger.Log(logEntry1);

        LogEntry logEntry2 = new LogEntry { Level = LogLevel.Error };
        logger.Log(logEntry2);

        LogEntry logEntry3 = new LogEntry();
        logEntry3.Level = LogLevel.Error;
        logger.Log(logEntry3);
    }
}").ConfigureAwait(false);
        }
    }
}
