using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class CaughtExceptionShouldBeLoggedAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenExceptionIsNotPassedToExtensionMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExceptionIsNotPassedToEntry()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
            var logEntry1 = new LogEntry(""A log without exception"", LogLevel.Info);
            [|logger.Log(logEntry1)|];

            var logEntry2 = new LogEntry
            {
                Message = ""A log without exception"",
                Level = LogLevel.Info
            };
            [|logger.Log(logEntry2)|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenNullIsPassedToExtensionMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
            [|logger.Debug(""A debug log with null exception"", null, new { foo = 123 })|];
            [|logger.Info(""An info log with null exception"", null, new { foo = 123 })|];
            [|logger.Warn(""A warn log with null exception"", null, new { foo = 123 })|];
            [|logger.Error(""An error log with null exception"", null, new { foo = 123 })|];
            [|logger.Fatal(""A fatal log with null exception"", null, new { foo = 123 })|];
            [|logger.Audit(""An audit log with null exception"", null, new { foo = 123 })|];

            [|logger.DebugSanitized(""A debug log with null exception"", null, new { foo = 123 })|];
            [|logger.InfoSanitized(""An info log with null exception"", null, new { foo = 123 })|];
            [|logger.WarnSanitized(""A warn log with null exception"", null, new { foo = 123 })|];
            [|logger.ErrorSanitized(""An error log with null exception"", null, new { foo = 123 })|];
            [|logger.FatalSanitized(""A fatal log with null exception"", null, new { foo = 123 })|];
            [|logger.AuditSanitized(""An audit log with null exception"", null, new { foo = 123 })|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenNullIsPassedToEntry()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
            var logEntry1 = new LogEntry(""A log with null exception"", null, LogLevel.Info);
            [|logger.Log(logEntry1)|];

            var logEntry2 = new LogEntry
            {
                Message = ""A log without exception"",
                Level = LogLevel.Info,
                Exception = null
            };
            [|logger.Log(logEntry2)|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenIncorrectExceptionIsPassedToExtensionMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        var someOtherException = new InvalidOperationException(""Some other exception"");
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            [|logger.Debug(""A debug log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.Info(""An info log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.Warn(""A warn log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.Error(""An error log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.Fatal(""A fatal log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.Audit(""An audit log with some other exception"", someOtherException, new { foo = 123 })|];

            [|logger.DebugSanitized(""A debug log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.InfoSanitized(""An info log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.WarnSanitized(""A warn log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.ErrorSanitized(""An error log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.FatalSanitized(""A fatal log with some other exception"", someOtherException, new { foo = 123 })|];
            [|logger.AuditSanitized(""An audit log with some other exception"", someOtherException, new { foo = 123 })|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenIncorrectExceptionIsPassedToEntry()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        var someOtherException = new InvalidOperationException(""Some other exception"");
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            var logEntry1 = new LogEntry(""A log with some other exception"", someOtherException, LogLevel.Info);
            [|logger.Log(logEntry1)|];

            var logEntry2 = new LogEntry
            {
                Message = ""A log without exception"",
                Level = LogLevel.Info,
                Exception = someOtherException
            };
            [|logger.Log(logEntry2)|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenMultipleExceptionsAreNotPassedToExtensionMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Net;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (ArgumentException argEx)
        {
            var log = new LogEntry(""message"", LogLevel.Error);
             [|logger.Log(log)|];
        }
        catch (WebException webEx)
        {
            var log = new LogEntry(""message"", LogLevel.Error);
            [|logger.Log(log)|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenIncorrectExceptionIsPassedToLogMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Net;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        var someOtherException = new InvalidOperationException(""Some other exception"");
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (ArgumentException argEx)
        {
            [|logger.Error(""ope, an error occurred"", someOtherException)|];
            [|logger.Info(""ope, an error occurred"", someOtherException)|];
            [|logger.Debug(""ope, an error occurred"", someOtherException)|];
            [|logger.Warn(""ope, an error occurred"", someOtherException)|];
            [|logger.Audit(""ope, an error occurred"", someOtherException)|];

            [|logger.ErrorSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.InfoSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.DebugSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.WarnSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.AuditSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
        }
        catch (WebException webEx)
        {
            [|logger.Error(""ope, an error occurred"", someOtherException)|];
            [|logger.Info(""ope, an error occurred"", someOtherException)|];
            [|logger.Debug(""ope, an error occurred"", someOtherException)|];
            [|logger.Warn(""ope, an error occurred"", someOtherException)|];
            [|logger.Audit(""ope, an error occurred"", someOtherException)|];

            [|logger.ErrorSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.InfoSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.DebugSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.WarnSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
            [|logger.AuditSanitized(""ope, an error occurred"", someOtherException, new { foo = 123 })|];
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenCaughtExceptionIsPassedCorrectlyToExtensionMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenCaughtExceptionIsPassedCorrectlyToEntry()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
        catch (ArgumentException ex)
        {
            var logEntry = new LogEntry(""A log without exception"", ex, LogLevel.Info);
            logger.Log(logEntry);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenCaughtExceptionIsPassedCorrectlyToConstructor()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
        catch (ArgumentException ex)
        {
            var logEntry = new LogEntry(""A log without exception"", LogLevel.Info)
            {
                Exception = ex
            };
            logger.Log(logEntry);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenCaughtExceptionIsPassedCorrectlyToPropertySetter()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenCaughtExceptionIsFilteredAndLogged()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
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
        catch (ArgumentException ex)
        {
            logger.Debug(""A debug log without exception"", ex);
            logger.Info(""An info log without exception"", ex);
            logger.Warn(""A warn log without exception"", ex);
            logger.Error(""An error log without exception"", ex);
            logger.Fatal(""A fatal log without exception"", ex);
            logger.Audit(""An audit log without exception"", ex);

            logger.DebugSanitized(""A debug log without exception"", ex, new { foo = 123 });
            logger.InfoSanitized(""An info log without exception"", ex, new { foo = 123 });
            logger.WarnSanitized(""A warn log without exception"", ex, new { foo = 123 });
            logger.ErrorSanitized(""An error log without exception"", ex, new { foo = 123 });
            logger.FatalSanitized(""A fatal log without exception"", ex, new { foo = 123 });
            logger.AuditSanitized(""An audit log without exception"", ex, new { foo = 123 });
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenMutlipleCaughtExceptionIsFilteredAndLogged()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Net;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (ArgumentException argEx)
        {
            var log = new LogEntry(""message"", argEx, LogLevel.Error);
            logger.Log(log);
        }
        catch (WebException webEx)
        {
            var log = new LogEntry(""message"", webEx, LogLevel.Error);
            logger.Log(log);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenMultipleCaughtExceptionsAreLogged()
        {
            await TestAssistants.VerifyAnalyzerAsync<CaughtExceptionShouldBeLoggedAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Net;

public class Test
{
    public void Call_Log_Within_Catch_Block(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (ArgumentException argEx)
        {
            logger.Error(""ope, an error occurred"", argEx);
            logger.Info(""ope, an error occurred"", argEx);
            logger.Debug(""ope, an error occurred"", argEx);
            logger.Warn(""ope, an error occurred"", argEx);
            logger.Audit(""ope, an error occurred"", argEx);

            logger.ErrorSanitized(""ope, an error occurred"", argEx, new { foo = 123 });
            logger.InfoSanitized(""ope, an error occurred"", argEx, new { foo = 123 });
            logger.DebugSanitized(""ope, an error occurred"", argEx, new { foo = 123 });
            logger.WarnSanitized(""ope, an error occurred"", argEx, new { foo = 123 });
            logger.AuditSanitized(""ope, an error occurred"", argEx, new { foo = 123 });
        }
        catch (WebException webEx)
        {
            logger.Error(""ope, an error occurred"", webEx);
            logger.Info(""ope, an error occurred"", webEx);
            logger.Debug(""ope, an error occurred"", webEx);
            logger.Warn(""ope, an error occurred"", webEx);
            logger.Audit(""ope, an error occurred"", webEx);

            logger.ErrorSanitized(""ope, an error occurred"", webEx, new { foo = 123 });
            logger.InfoSanitized(""ope, an error occurred"", webEx, new { foo = 123 });
            logger.DebugSanitized(""ope, an error occurred"", webEx, new { foo = 123 });
            logger.WarnSanitized(""ope, an error occurred"", webEx, new { foo = 123 });
            logger.AuditSanitized(""ope, an error occurred"", webEx, new { foo = 123 });
        }
    }
}").ConfigureAwait(false);
        }
    }
}
