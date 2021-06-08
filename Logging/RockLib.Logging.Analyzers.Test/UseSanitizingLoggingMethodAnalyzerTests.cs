using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.UseSanitizingLoggingMethodAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    [TestClass]
    public class UseSanitizingLoggingMethodAnalyzerTests
    {
        [TestMethod("Diagnostrics are reported when setting extended property with a non-value type")]
        public async Task DiagnosticsReported()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: "Foo",
                    shouldReportDiagnostic: true));
        }

        [DataTestMethod]
        [DataRow("string", DisplayName = "No diagnostics are reported for extended properties of type string")]
        [DataRow("bool", DisplayName = "No diagnostics are reported for extended properties of type bool")]
        [DataRow("char", DisplayName = "No diagnostics are reported for extended properties of type char")]
        [DataRow("short", DisplayName = "No diagnostics are reported for extended properties of type short")]
        [DataRow("int", DisplayName = "No diagnostics are reported for extended properties of type int")]
        [DataRow("long", DisplayName = "No diagnostics are reported for extended properties of type long")]
        [DataRow("ushort", DisplayName = "No diagnostics are reported for extended properties of type ushort")]
        [DataRow("uint", DisplayName = "No diagnostics are reported for extended properties of type uint")]
        [DataRow("ulong", DisplayName = "No diagnostics are reported for extended properties of type ulong")]
        [DataRow("byte", DisplayName = "No diagnostics are reported for extended properties of type byte")]
        [DataRow("sbyte", DisplayName = "No diagnostics are reported for extended properties of type sbyte")]
        [DataRow("float", DisplayName = "No diagnostics are reported for extended properties of type float")]
        [DataRow("double", DisplayName = "No diagnostics are reported for extended properties of type double")]
        [DataRow("decimal", DisplayName = "No diagnostics are reported for extended properties of type decimal")]
        [DataRow("DateTime", DisplayName = "No diagnostics are reported for extended properties of type DateTime")]
        [DataRow("IntPtr", DisplayName = "No diagnostics are reported for extended properties of type IntPtr")]
        [DataRow("UIntPtr", DisplayName = "No diagnostics are reported for extended properties of type UIntPtr")]
        [DataRow("TimeSpan", DisplayName = "No diagnostics are reported for extended properties of type TimeSpan")]
        [DataRow("DateTimeOffset", DisplayName = "No diagnostics are reported for extended properties of type DateTimeOffset")]
        [DataRow("Guid", DisplayName = "No diagnostics are reported for extended properties of type Guid")]
        [DataRow("Uri", DisplayName = "No diagnostics are reported for extended properties of type Uri")]
        [DataRow("Encoding", DisplayName = "No diagnostics are reported for extended properties of type Encoding")]
        [DataRow("Type", DisplayName = "No diagnostics are reported for extended properties of type Type")]
        [DataRow("TypeCode", DisplayName = "No diagnostics are reported for extended properties of an enum type")]
        public async Task NoDiagnosticsReported(string extendedPropertyType)
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: extendedPropertyType,
                    shouldReportDiagnostic: false));
        }


        private static string GetTestCode(string extendedPropertyType, bool shouldReportDiagnostic)
        {
            string openDiagnostic = shouldReportDiagnostic ? "[|" : null;
            string closeDiagnostic = shouldReportDiagnostic ? "|]" : null;

            return string.Format(@"
using RockLib.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalyzerTests
{{
    public class Foo
    {{
        public string Bar {{ get; set; }}

        public void Baz(ILogger logger, {0} foo)
        {{
            // Set extended property value with indexer
            var logEntry = new LogEntry();
            {1}logEntry.ExtendedProperties[""bar""] = foo{2};

            // Set extended properties with in-line anonymous object in LogEntry constructor
            var logEntry1 = new LogEntry(""Hello, world!"", {1}extendedProperties: new {{ foo }}{2});

            // Set extended properties with anonymous object variable in LogEntry constructor
            var properties2 = new {{ foo }};
            var logEntry2 = new LogEntry(""Hello, world!"", {1}extendedProperties: properties2{2});

            // Set extended properties with dictionary initialized with Add method in LogEntry constructor
            var properties3 = new Dictionary<string, object>
            {{
                {{ ""foo"", foo }}
            }};
            properties3.Add(""bar"", foo);
            var logEntry3 = new LogEntry(""Hello, world!"", {1}extendedProperties: properties3{2});

            // Set extended properties with dictionary initialized with indexer in LogEntry constructor
            var properties4 = new Dictionary<string, object>
            {{
                [""foo""] = foo
            }};
            properties4[""bar""] = foo;
            var logEntry4 = new LogEntry(""Hello, world!"", {1}extendedProperties: properties4{2});

            // Set extended properties in-line anonymous object in logging extension method
            logger.Audit(""Hello, world!"", {1}extendedProperties: new {{ foo }}{2});

            // Set extended properties with anonymous object variable in logging extension method
            var properties5 = new {{ foo }};
            logger.Fatal(""Hello, world!"", {1}extendedProperties: properties5{2});

            // Set extended properties with dictionary initialized with Add method in logging extension method
            var properties6 = new Dictionary<string, object>
            {{
                {{ ""foo"", foo }}
            }};
            properties6.Add(""bar"", foo);
            logger.Error(""Hello, world!"", {1}extendedProperties: properties6{2});

            // Set extended properties with dictionary initialized with indexer in logging extension method
            var properties7 = new Dictionary<string, object>
            {{
                [""foo""] = foo
            }};
            properties7[""bar""] = foo;
            logger.Warn(""Hello, world!"", {1}extendedProperties: properties7{2});

            // Set extended properties with in-line anonymous object in SetExtendedProperties method
            var logEntry8 = new LogEntry();
            {1}logEntry8.SetExtendedProperties(new {{ foo }}){2};

            // Set extended properties with anonymous object variable in SetExtendedProperties method
            var logEntry9 = new LogEntry();
            var properties9 = new {{ foo }};
            {1}logEntry9.SetExtendedProperties(properties9){2};

            // Set extended properties with dictionary initialized with Add method in SetExtendedProperties method
            var logEntry10 = new LogEntry();
            var properties10 = new Dictionary<string, object>
            {{
                {{ ""foo"", foo }}
            }};
            properties10.Add(""bar"", foo);
            {1}logEntry10.SetExtendedProperties(properties10){2};

            // Set extended properties with dictionary initialized with indexer in SetExtendedProperties method
            var logEntry11 = new LogEntry();
            var properties11 = new Dictionary<string, object>
            {{
                [""foo""] = foo
            }};
            properties11[""bar""] = foo;
            {1}logEntry11.SetExtendedProperties(properties11){2};
        }}
    }}
}}", extendedPropertyType, openDiagnostic, closeDiagnostic);
        }
    }
}
