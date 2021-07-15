using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.CaughtExceptionShouldBeLoggedAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    public class CaughtExceptionShouldBeLoggedAnalyzerTests
    {
        [Fact(DisplayName = "Diagnostics are reported when exception is not passed to logging extension methods")]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            [|logger.Debug(""A debug log without exception"")|];
            [|logger.Info(""An info log without exception"")|];
            [|logger.Warn(""A warn log without exception"")|];
            [|logger.Error(""An error log without exception"")|];
            [|logger.Fatal(""A fatal log without exception"")|];
            [|logger.Audit(""An audit log without exception"")|];

            [|logger.DebugSanitized(""A debug log without exception"", new { foo = 123 })|];
            [|logger.InfoSanitized(""An info log without exception"", new { foo = 123 })|];
            [|logger.WarnSanitized(""A warn log without exception"", new { foo = 123 })|];
            [|logger.ErrorSanitized(""An error log without exception"", new { foo = 123 })|];
            [|logger.FatalSanitized(""A fatal log without exception"", new { foo = 123 })|];
            [|logger.AuditSanitized(""An audit log without exception"", new { foo = 123 })|];
        }
    }
}");
        }

        [Fact(DisplayName = "Diagnostics are reported when exception is not passed to log entry")]
        public async Task DiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            var logEntry = new LogEntry(""A log without exception"", LogLevel.Info);
            [|logger.Log(logEntry)|];
        }
    }
}");
        }

        [Fact(DisplayName = "No diagnostics are reported when exception is passed to logging extension methods")]
        public async Task NoDiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            logger.Debug(""A debug log with exception"", ex);
            logger.Info(""An info log with exception"", ex);
            logger.Warn(""A warn log with exception"", ex);
            logger.Error(""An error log with exception"", ex);
            logger.Fatal(""A fatal log with exception"", ex);
            logger.Audit(""An audit log with exception"", ex);

            logger.DebugSanitized(""A debug log with exception"", ex, new { foo = 123 });
            logger.InfoSanitized(""An info log with exception"", ex, new { foo = 123 });
            logger.WarnSanitized(""A warn log with exception"", ex, new { foo = 123 });
            logger.ErrorSanitized(""An error log with exception"", ex, new { foo = 123 });
            logger.FatalSanitized(""A fatal log with exception"", ex, new { foo = 123 });
            logger.AuditSanitized(""An audit log with exception"", ex, new { foo = 123 });
        }
    }
}");
        }

        [Fact(DisplayName = "No diagnostics are reported when exception is passed to log entry constructor")]
        public async Task NoDiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            var logEntry = new LogEntry(""A log without exception"", ex, LogLevel.Info);
            logger.Log(logEntry);
        }
    }
}");
        }

        [Fact(DisplayName = "No diagnostics are reported when exception is passed to log entry constructor initializer")]
        public async Task NoDiagnosticsReported3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            var logEntry = new LogEntry(""A log without exception"", LogLevel.Info)
            {
                Exception = ex
            };
            logger.Log(logEntry);
        }
    }
}");
        }

        [Fact(DisplayName = "No diagnostics are reported when exception is passed to log entry property setter")]
        public async Task NoDiagnosticsReported4()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            var logEntry = new LogEntry(""A log without exception"", LogLevel.Info);
            logEntry.Exception = ex;
            logger.Log(logEntry);
        }
    }
}");
        }
    }
}
