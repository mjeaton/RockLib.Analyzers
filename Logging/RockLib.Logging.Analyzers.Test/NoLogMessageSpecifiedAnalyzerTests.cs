using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.NoLogMessageSpecifiedAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    public class NoLogMessageSpecifiedAnalyzerTests
    {
        [Fact(DisplayName = "Diagnostics are not reported when log message is provided")]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Set(ILogger logger)
    {
        logger.Log(new LogEntry(""i won't tell you again""));
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when log message is not provided in ctor")]
        public async Task DiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        var entry = new  LogEntry("""", LogLevel.Info);
        logger.Log([|entry|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when log message is not provided in LogEntry initializer")]
        public async Task DiagnosticsReported3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        LogEntry logEntry1 = new LogEntry(){Level = LogLevel.Debug}; 
        logger.Log([|logEntry1|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when an empty log message is not provided in LogEntry initializer")]
        public async Task DiagnosticsReported4()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        LogEntry logEntry1 = new LogEntry(){Message = """", Level = LogLevel.Debug};
        logger.Log([|logEntry1|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when a log message is not set and symbols match")]
        public async Task DiagnosticsReported5()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        LogEntry log = new LogEntry();
        log.Level = LogLevel.Debug;
        log.Message = """";
        logger.Log([|log|]);

        LogEntry log2 = new LogEntry();
        log2.Level = LogLevel.Debug;
        log2.Message = ""weird"";
        logger.Log(log2);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are not reported when an log message is set in property")]
        public async Task DiagnosticsReported6()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        LogEntry log = new LogEntry();
        log.Level = LogLevel.Debug;
        log.Message = ""no problemz here."";
        logger.Log(log);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when log message is empty in logging method")]
        public async Task DiagnosticsReported7()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        logger.Info([|""""|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are not reported when log message is provided")]
        public async Task DiagnosticsReported8()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {        
        logger.Info(""graveler"");
    }
}");
        }
    }
}