using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.ExtendedPropertyNotMarkedSafeToLogAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    [TestClass]
    public class ExtendedPropertyNotMarkedSafeToLogAnalyzerTests
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

        public void Baz(LogEntry logEntry)
        {
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

        public void Baz(LogEntry logEntry)
        {
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

        public void Baz(LogEntry logEntry)
        {
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

        public void Baz(LogEntry logEntry)
        {
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

        [DataTestMethod]
        [DataRow("string")]
        [DataRow("bool")]
        [DataRow("char")]
        [DataRow("short")]
        [DataRow("int")]
        [DataRow("long")]
        [DataRow("ushort")]
        [DataRow("uint")]
        [DataRow("ulong")]
        [DataRow("byte")]
        [DataRow("sbyte")]
        [DataRow("float")]
        [DataRow("double")]
        [DataRow("decimal")]
        [DataRow("DateTime")]
        [DataRow("IntPtr")]
        [DataRow("UIntPtr")]
        [DataRow("Garply")]
        [DataRow("TimeSpan")]
        [DataRow("DateTimeOffset")]
        [DataRow("Guid")]
        [DataRow("Uri")]
        [DataRow("Encoding")]
        [DataRow("Type")]
        public async Task NoDiagnostics4(string propertyType)
        {
            await RockLibVerifier.VerifyAnalyzerAsync(string.Format(@"
using RockLib.Logging;
using System;
using System.Text;

namespace AnalyzerTests
{{
    public class Foo
    {{
        public string Bar {{ get; set; }}

        public void Baz(LogEntry logEntry, {0} foo)
        {{
            logEntry.SetSanitizedExtendedProperty(""foo"", foo);
        }}
    }}

    public enum Garply
    {{
        Grault
    }}
}}", propertyType));
        }
    }
}
