using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class NoLogMessageSpecifiedAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenLogMessageIsProvided()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Set(ILogger logger)
    {
        logger.Log(new LogEntry(""i won't tell you again""));
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsProvidedToSpecificLogLevelMethods()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsProvidedViaProperty()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsNotProvided()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsNotProvidedInEntry()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
using System;

public class Test
{
    public void Call_Log_With_LogEntry_With_Message_Not_Set(ILogger logger)
    {
        LogEntry logEntry1 = new LogEntry(){Level = LogLevel.Debug}; 
        logger.Log([|logEntry1|]);
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsProvidedAsEmpty()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsNotSet()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLogMessageIsEmpty()
        {
            await TestAssistants.VerifyAnalyzerAsync<NoLogMessageSpecifiedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }
    }
}