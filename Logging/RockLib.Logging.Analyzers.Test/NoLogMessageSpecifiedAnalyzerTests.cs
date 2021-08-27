using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.NoLogMessageSpecifiedAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    public class NoLogMessageSpecifiedAnalyzerTests
    {
        [Fact(DisplayName = "Diagnostics are not reported when log message is provided")]
        public async Task NoDiagnosticsReported1()
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

        [Fact(DisplayName = "Diagnostics are not reported when log message is provided")]
        public async Task NoDiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        logger.Info(""geodude"");
        logger.Debug(""geodude"");
        logger.Warn(""geodude"");
        logger.Error(""geodude"");
        logger.Audit(""geodude"");

        logger.InfoSanitized(""graveler"", new { value = 123 });
        logger.DebugSanitized(""graveler"", new { value = 123 });
        logger.WarnSanitized(""graveler"", new { value = 123 });
        logger.ErrorSanitized(""graveler"", new { value = 123 });
        logger.AuditSanitized(""graveler"", new { value = 123 });
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are not reported when an log message is set in property")]
        public async Task NoDiagnosticsReported3()
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

        [Fact(DisplayName = "Diagnostics are reported when log message is not provided in ctor")]
        public async Task DiagnosticsReported1()
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

        var entry2 = new  LogEntry(null, LogLevel.Info);
        logger.Log([|entry2|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when log message is not provided in LogEntry initializer")]
        public async Task DiagnosticsReported2()
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
        public async Task DiagnosticsReported3()
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

        LogEntry logEntry2 = new LogEntry(){Message = null, Level = LogLevel.Debug};
        logger.Log([|logEntry2|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when a log message is not set and symbols match")]
        public async Task DiagnosticsReported4()
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
        log2.Message = null;
        logger.Log([|log2|]);
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when log message is empty in logging method")]
        public async Task DiagnosticsReported7()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        logger.Info([|""""|]);
        logger.Debug([|""""|]);
        logger.Warn([|""""|]);
        logger.Error([|""""|]);
        logger.Audit([|""""|]);

        logger.Info([|null|]);
        logger.Debug([|null|]);
        logger.Warn([|null|]);
        logger.Error([|null|]);
        logger.Audit([|null|]);

        logger.InfoSanitized([|""""|], new { value = 456 });
        logger.DebugSanitized([|""""|], new { value = 456 });
        logger.WarnSanitized([|""""|], new { value = 456 });
        logger.ErrorSanitized([|""""|], new { value = 456 });
        logger.AuditSanitized([|""""|], new { value = 456 });

        logger.InfoSanitized([|null|], new { value = 456 });
        logger.DebugSanitized([|null|], new { value = 456 });
        logger.WarnSanitized([|null|], new { value = 456 });
        logger.ErrorSanitized([|null|], new { value = 456 });
        logger.AuditSanitized([|null|], new { value = 456 });
    }
}");
        }
    }
}