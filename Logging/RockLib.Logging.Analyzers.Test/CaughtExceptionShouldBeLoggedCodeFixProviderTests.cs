using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Analyzers.CaughtExceptionShouldBeLoggedAnalyzer,
    RockLib.Logging.Analyzers.CaughtExceptionShouldBeLoggedCodeFixProvider>;

namespace RockLib.Logging.Analyzers.Test
{
    public class CaughtExceptionShouldBeLoggedCodeFixProviderTests
    {
        [Fact(DisplayName = null)]
        public async Task CodeFixApplied1()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            // Logging extension methods
            [|logger.Error(""Hello, world!"")|];
            [|logger.ErrorSanitized(""Hello, world!"", new { Foo = 123 })|];

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error
            };
            [|logger.Log(logEntry1)|];

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", LogLevel.Error);
            [|logger.Log(logEntry2)|];
        }
    }
}", @"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            // Logging extension methods
            logger.Error(""Hello, world!"", ex);
            logger.ErrorSanitized(""Hello, world!"", ex, new { Foo = 123 });

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            [|logger.Log(logEntry2)|];
        }
    }
}");
        }

        [Fact(DisplayName = null)]
        public async Task CodeFixApplied2()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception)
        {
            // Logging extension methods
            [|logger.Error(""Hello, world!"")|];
            [|logger.ErrorSanitized(""Hello, world!"", new { Foo = 123 })|];

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error
            };
            [|logger.Log(logEntry1)|];

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", LogLevel.Error);
            [|logger.Log(logEntry2)|];
        }
    }
}", @"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            // Logging extension methods
            logger.Error(""Hello, world!"", ex);
            logger.ErrorSanitized(""Hello, world!"", ex, new { Foo = 123 });

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            logger.Log(logEntry2);
        }
    }
}");
        }

        [Fact(DisplayName = null)]
        public async Task CodeFixApplied3()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch
        {
            // Logging extension methods
            [|logger.Error(""Hello, world!"")|];
            [|logger.ErrorSanitized(""Hello, world!"", new { Foo = 123 })|];

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error
            };
            [|logger.Log(logEntry1)|];

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", LogLevel.Error);
            [|logger.Log(logEntry2)|];
        }
    }
}", @"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
            throw new ArgumentException(""This is a test"");
        }
        catch (Exception ex)
        {
            // Logging extension methods
            logger.Error(""Hello, world!"", ex);
            logger.ErrorSanitized(""Hello, world!"", ex, new { Foo = 123 });

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            logger.Log(logEntry2);
        }
    }
}");
        }
    }
}
