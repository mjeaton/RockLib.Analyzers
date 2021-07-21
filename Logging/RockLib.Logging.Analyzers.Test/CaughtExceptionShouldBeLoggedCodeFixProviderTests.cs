using System.Threading.Tasks;
using Xunit;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Analyzers.CaughtExceptionShouldBeLoggedAnalyzer,
    RockLib.Logging.Analyzers.CaughtExceptionShouldBeLoggedCodeFixProvider>;

namespace RockLib.Logging.Analyzers.Test
{
    public class CaughtExceptionShouldBeLoggedCodeFixProviderTests
    {
        [Fact(DisplayName = "Code fix adds catch exception variable to logs")]
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

            // Named arguments
            [|logger.Error(message: ""Hello, world!"")|];
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error);
            [|logger.Log(logEntry3)|];
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

            // Named arguments
            logger.Error(message: ""Hello, world!"", exception: ex);
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}");
        }

        [Fact(DisplayName = "Code fix adds missing variable for catch declaration")]
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

            // Named arguments
            [|logger.Error(message: ""Hello, world!"")|];
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error);
            [|logger.Log(logEntry3)|];
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

            // Named arguments
            logger.Error(message: ""Hello, world!"", exception: ex);
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}");
        }

        [Fact(DisplayName = "Code fix adds missing catch declaration")]
        public async Task CodeFixApplied3()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
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

            // Named arguments
            [|logger.Error(message: ""Hello, world!"")|];
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error);
            [|logger.Log(logEntry3)|];
        }
    }
}", @"
using System;
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

public class Test
{
    public void Log_With_Exception_Not_Set(ILogger logger)
    {
        try
        {
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

            // Named arguments
            logger.Error(message: ""Hello, world!"", exception: ex);
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}");
        }

        [Fact(DisplayName = "Code fix replaces null exception parameters with catch variable")]
        public async Task CodeFixApplied4()
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
        }
        catch (Exception ex)
        {
            // Logging extension methods
            [|logger.Error(""Hello, world!"", null, new { Foo = 123 })|];
            [|logger.ErrorSanitized(""Hello, world!"", null, new { Foo = 123 })|];

            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = null
            };
            [|logger.Log(logEntry1)|];

            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", null, LogLevel.Error);
            [|logger.Log(logEntry2)|];

            // Named arguments
            [|logger.Error(message: ""Hello, world!"", exception: null)|];
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: null);
            [|logger.Log(logEntry3)|];
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
        }
        catch (Exception ex)
        {
            // Logging extension methods
            logger.Error(""Hello, world!"", ex, new { Foo = 123 });
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

            // Named arguments
            logger.Error(message: ""Hello, world!"", exception: ex);
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}");
        }
    }
}
