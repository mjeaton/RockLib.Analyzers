using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.Analyzers.ExtendedPropertyNotMarkedSafeToLogAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
    [TestClass]
    public class ExtendedPropertyNotMarkedSafeToLogAnalyzerTests
    {
        [TestMethod("Diagnostrics are reported when extended property type is not marked as safe to log")]
        public async Task DiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: "Foo",
                    shouldReportDiagnostic: true,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None));
        }

        [TestMethod("Diagnostrics are reported when extended property type is decorated with [SafeToLog] but all properties are decorated with [NotSafeToLog]")]
        public async Task DiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: "Foo",
                    shouldReportDiagnostic: true,
                    propertyDecoration: Decoration.NotSafeToLog,
                    classDecoration: Decoration.SafeToLog));
        }

        [TestMethod("No diagnostics are reported when extended property type has property decorated with [SafeToLog]")]
        public async Task NoDiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: "Foo",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.SafeToLog,
                    classDecoration: Decoration.None));
        }

        [TestMethod("No diagnostics are reported when extended property type is decorated with [SafeToLog]")]
        public async Task NoDiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: "Foo",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.SafeToLog));
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
        public async Task NoDiagnosticsReported3(string extendedPropertyType)
        {
            await RockLibVerifier.VerifyAnalyzerAsync(
                GetTestCode(
                    extendedPropertyType: extendedPropertyType,
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None));
        }

        private static string GetTestCode(string extendedPropertyType, bool shouldReportDiagnostic,
            Decoration propertyDecoration, Decoration classDecoration)
        {
            string openDiagnostic = shouldReportDiagnostic ? "[|" : null;
            string closeDiagnostic = shouldReportDiagnostic ? "|]" : null;
            string propertyDecorationValue = propertyDecoration != Decoration.None ? $"[{propertyDecoration}]" : null;
            string classDecorationValue = classDecoration != Decoration.None ? $"[{classDecoration}]" : null;

            return string.Format(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalyzerTests
{{
    {4}
    public class Foo
    {{
        {3}
        public string Bar {{ get; set; }}

        public void Baz(ILogger logger, LogEntry logEntry, {0} foo)
        {{
            // Setting a single extended property
            logEntry.SetSanitizedExtendedProperty(""foo"", {1}foo{2});

            // Setting extended properties in-line with an anonymous object
            logEntry.SetSanitizedExtendedProperties(new {{ {1}foo{2} }});
            logger.DebugSanitized(""Hello, world!"", new {{ {1}foo{2} }});

            // Setting extended properties with an anonymous object variable
            var properties1 = new {{ {1}foo{2} }};
            logEntry.SetSanitizedExtendedProperties(properties1);

            var properties2 = new {{ {1}foo{2} }};
            logger.InfoSanitized(""Hello, world!"", properties2);

            // Setting extended properties with a dictionary populated with Add method
            var properties3 = new Dictionary<string, object>
            {{
                {{ ""foo"", {1}foo{2} }}
            }};
            properties3.Add(""bar"", {1}foo{2});
            logEntry.SetSanitizedExtendedProperties(properties3);

            var properties4 = new Dictionary<string, object>
            {{
                {{ ""foo"", {1}foo{2} }}
            }};
            properties4.Add(""bar"", {1}foo{2});
            logger.WarnSanitized(""Hello, world!"", properties4);

            // Setting extended properties with a dictionary populated with indexer
            var properties5 = new Dictionary<string, object>
            {{
                [""foo""] = {1}foo{2}
            }};
            properties5[""bar""] = {1}foo{2};
            logEntry.SetSanitizedExtendedProperties(properties5);

            var properties6 = new Dictionary<string, object>
            {{
                [""foo""] = {1}foo{2}
            }};
            properties6[""bar""] = {1}foo{2};
            logger.ErrorSanitized(""Hello, world!"", properties6);
        }}
    }}
}}", extendedPropertyType, openDiagnostic, closeDiagnostic, propertyDecorationValue, classDecorationValue);
        }

        private enum Decoration
        {
            None,
            SafeToLog,
            NotSafeToLog
        }
    }
}
