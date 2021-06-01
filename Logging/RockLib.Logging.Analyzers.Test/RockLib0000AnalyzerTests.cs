using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.RockLib0000Analyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    [TestClass]
    public class RockLib0000AnalyzerTests
    {
        [TestMethod]
        public async Task Analyzer1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;

namespace AnalyzerTests
{
    public class Foo
    {
        public string Bar { get; set; }

        public void Baz()
        {
            var logEntry = new LogEntry();
            logEntry.SetSanitizedExtendedProperty(""foo"", [|this|]);
        }
    }
}");
        }

        [TestMethod]
        public async Task Analyzer2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;

namespace AnalyzerTests
{
    public class Foo
    {
        public string Bar { get; set; }

        public void Baz()
        {
            var logEntry = new LogEntry();
            logEntry.SetSanitizedExtendedProperties(new { foo = [|this|] });
        }
    }
}");
        }

        [TestMethod]
        public async Task Analyzer3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

namespace AnalyzerTests
{
    public class Foo
    {
        public string Bar { get; set; }

        public void Baz(ILogger logger)
        {
            logger.DebugSanitized(""Hello, world!"", new { foo = [|this|] });
        }
    }
}");
        }

        [TestMethod]
        public async Task NoDiagnostics1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

namespace AnalyzerTests
{
    public class Foo
    {
        [SafeToLog]
        public string Bar { get; set; }

        public void Baz()
        {
            var logEntry = new LogEntry();
            logEntry.SetSanitizedExtendedProperty(""foo"", this);
        }
    }
}");
        }

        [TestMethod]
        public async Task NoDiagnostics2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

namespace AnalyzerTests
{
    public class Foo
    {
        [SafeToLog]
        public string Bar { get; set; }

        public void Baz()
        {
            var logEntry = new LogEntry();
            logEntry.SetSanitizedExtendedProperties(new { foo = this });
        }
    }
}");
        }

        [TestMethod]
        public async Task NoDiagnostics3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

namespace AnalyzerTests
{
    public class Foo
    {
        [SafeToLog]
        public string Bar { get; set; }

        public void Baz(ILogger logger)
        {
            logger.DebugSanitized(""Hello, world!"", new { foo = this });
        }
    }
}");
        }
    }
}
