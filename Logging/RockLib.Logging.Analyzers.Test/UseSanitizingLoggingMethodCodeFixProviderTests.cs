using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class UseSanitizingLoggingMethodCodeFixProviderTests
    {
        [Fact]
        public static async Task VerifyWhenSetExtendedPropertiesIsCalled()
        {
            await TestAssistants.VerifyCodeFixAsync<UseSanitizingLoggingMethodAnalyzer, UseSanitizingLoggingMethodCodeFixProvider>(
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        [|logEntry.SetExtendedProperties(new { foo = this })|];
    }
}", 
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        logEntry.SetSanitizedExtendedProperties(new { foo = this });
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenWarnIsCalled()
        {
            await TestAssistants.VerifyCodeFixAsync<UseSanitizingLoggingMethodAnalyzer, UseSanitizingLoggingMethodCodeFixProvider>(
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz(ILogger logger)
    {
        [|logger.Warn(""Hello, world!"", new { foo = this })|];
    }
}", 
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz(ILogger logger)
    {
        logger.WarnSanitized(""Hello, world!"", new { foo = this });
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenExtendedPropertiesAreProvided()
        {
            await TestAssistants.VerifyCodeFixAsync<UseSanitizingLoggingMethodAnalyzer, UseSanitizingLoggingMethodCodeFixProvider>(
@"using RockLib.Logging;
using System;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = [|new LogEntry(""Hello, world!"", extendedProperties: new { foo = this })
        {
            CorrelationId = Guid.NewGuid().ToString()
        }|];
    }
}", 
@"using RockLib.Logging;
using System;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry(""Hello, world!"")
        {
            CorrelationId = Guid.NewGuid().ToString()
        }.SetSanitizedExtendedProperties(new { foo = this });
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenExtendedPropertiesAreProvidedOnIndexer()
        {
            await TestAssistants.VerifyCodeFixAsync<UseSanitizingLoggingMethodAnalyzer, UseSanitizingLoggingMethodCodeFixProvider>(
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        [|logEntry.ExtendedProperties[""bar""] = this|];
    }
}", 
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        logEntry.SetSanitizedExtendedProperty(""bar"", this);
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task VerifyWhenExtendedPropertiesAddIsCalled()
        {
            await TestAssistants.VerifyCodeFixAsync<UseSanitizingLoggingMethodAnalyzer, UseSanitizingLoggingMethodCodeFixProvider>(
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        [|logEntry.ExtendedProperties.Add(""bar"", this)|];
    }
}", 
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        logEntry.SetSanitizedExtendedProperty(""bar"", this);
    }
}").ConfigureAwait(false);
        }

#if !NET48
        [Fact]
        public static async Task VerifyWhenExtendedPropertiesTryAddIsCalled()
        {
            await TestAssistants.VerifyCodeFixAsync<UseSanitizingLoggingMethodAnalyzer, UseSanitizingLoggingMethodCodeFixProvider>(
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        [|logEntry.ExtendedProperties.TryAdd(""bar"", this)|];
    }
}", 
@"using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        logEntry.SetSanitizedExtendedProperty(""bar"", this);
    }
}").ConfigureAwait(false);
        }
#endif
    }
}
