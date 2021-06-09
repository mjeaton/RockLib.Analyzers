using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.Analyzers.UseSanitizingLoggingMethodAnalyzer,
    RockLib.Logging.Analyzers.UseSanitizingLoggingMethodCodeFixProvider>;

namespace RockLib.Logging.Analyzers.Test
{
    [TestClass]
    public class UseSanitizingLoggingMethodCodeFixProviderTests
    {
        [TestMethod("'Change to SetSanitizedExtendedProperties' is applied")]
        public async Task CodeFixApplied1()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        [|logEntry.SetExtendedProperties(new { foo = this })|];
    }
}", @"
using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        logEntry.SetSanitizedExtendedProperties(new { foo = this });
    }
}");
        }

        [TestMethod("'Change to sanitizing logging extension method' is applied")]
        public async Task CodeFixApplied2()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz(ILogger logger)
    {
        [|logger.Warn(""Hello, world!"", new { foo = this })|];
    }
}", @"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz(ILogger logger)
    {
        logger.WarnSanitized(""Hello, world!"", new { foo = this });
    }
}");
        }

        [TestMethod("'Replace extendedProperties parameter with call to SetSanitizedExtendedProperties method' is applied")]
        public async Task CodeFixApplied3()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;
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
}", @"
using RockLib.Logging;
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
}");
        }

        [TestMethod("'Replace with call to SetSanitizedExtendedProperty' is applied")]
        public async Task CodeFixApplied4()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        [|logEntry.ExtendedProperties[""bar""] = this|];
    }
}", @"
using RockLib.Logging;

public class Foo
{
    public string Bar { get; set; }

    public void Baz()
    {
        var logEntry = new LogEntry();
        logEntry.SetSanitizedExtendedProperty(""bar"", this);
    }
}");
        }
    }
}
