using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class CaughtExceptionShouldBeLoggedCodeFixProviderTests
    {
        [Fact]
        public static async Task VerifyWhenExtensionMethodDoesNotUseException()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
        }
    }
}", 
@"using RockLib.Logging;
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
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenDefaultConstructorIsUsedIncorrectly()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error
            };
            [|logger.Log(logEntry1)|];
        }
    }
}", 
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenCustomConstructorIsUsedIncorrectly()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", LogLevel.Error);
            [|logger.Log(logEntry2)|];

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error);
            [|logger.Log(logEntry3)|];
        }
    }
}", 
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            logger.Log(logEntry2);

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenExtensionMethodDoesNotUseExceptionAndExceptionVariableDoesNotExist()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
        }
    }
}", 
@"using RockLib.Logging;
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
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenDefaultConstructorIsUsedIncorrectlyAndExceptionVariableDoesNotExist()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error
            };
            [|logger.Log(logEntry1)|];
        }
    }
}", 
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenCustomConstructorIsUsedIncorrectlyAndExceptionVariableDoesNotExist()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", LogLevel.Error);
            [|logger.Log(logEntry2)|];

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error);
            [|logger.Log(logEntry3)|];
        }
    }
}", 
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            logger.Log(logEntry2);

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenExtensionMethodDoesNotUseExceptionWithEmptyCatchClause()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
        }
    }
}", 
@"using System;
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
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenDefaultConstructorIsUsedIncorrectlyWithEmptyCatchClause()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error
            };
            [|logger.Log(logEntry1)|];
        }
    }
}", 
@"using System;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenCustomConstructorIsUsedIncorrectlyWithEmptyCatchClause()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", LogLevel.Error);
            [|logger.Log(logEntry2)|];

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error);
            [|logger.Log(logEntry3)|];
        }
    }
}", 
@"using System;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            logger.Log(logEntry2);

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenExtensionMethodPassesNull()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
        }
    }
}", 
@"using RockLib.Logging;
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
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenDefaultConstructorPassesNull()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = null
            };
            [|logger.Log(logEntry1)|];
        }
    }
}", 
@"using RockLib.Logging;
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
            // Default constructor
            var logEntry1 = new LogEntry
            {
                Message = ""Hello, world!"",
                Level = LogLevel.Error,
                Exception = ex
            };
            logger.Log(logEntry1);
        }
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenCustomConstructorPassesNull()
        {
            await TestAssistants.VerifyCodeFixAsync<CaughtExceptionShouldBeLoggedAnalyzer, CaughtExceptionShouldBeLoggedCodeFixProvider>(
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", null, LogLevel.Error);
            [|logger.Log(logEntry2)|];

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: null);
            [|logger.Log(logEntry3)|];
        }
    }
}", 
@"using RockLib.Logging;
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
            // Non-default constructor
            var logEntry2 = new LogEntry(""Hello, world!"", ex, LogLevel.Error);
            logger.Log(logEntry2);

            // Named arguments
            var logEntry3 = new LogEntry(message: ""Hello, world!"", level: LogLevel.Error, exception: ex);
            logger.Log(logEntry3);
        }
    }
}").ConfigureAwait(false);
        }
    }
}