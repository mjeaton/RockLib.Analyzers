using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.NoLogLevelSpecifiedAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    [TestClass]
    public class NoLogLevelSpecifiedAnalyzerTests
    {
        [TestMethod]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
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
}");
        }

        [TestMethod]
        public async Task NoDiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
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
}");
        }
    }
}
