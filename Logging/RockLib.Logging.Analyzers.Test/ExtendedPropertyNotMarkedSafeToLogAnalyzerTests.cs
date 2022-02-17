#if !NET48
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class ExtendedPropertyNotMarkedSafeToLogAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsNotMarkedAsSafeToLog()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: true,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None)).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsDecoratedAsSafeButAllPropertiesAreDecoratedAsNotSafe()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: true,
                    propertyDecoration: Decoration.NotSafeToLog,
                    classDecoration: Decoration.SafeToLog)).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsDecoratedAsSafeAtRuntimeButAllPropertiesAreDecoratedAsNotSafe()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: true,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None) + @"

public class Program
{
    public static void Main(string[] args)
    {
        SafeToLogAttribute.Decorate<TestClass>();
        NotSafeToLogAttribute.Decorate<TestClass>(testClass => testClass.ExampleProperty);
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeHasPropertyDecoratedAsSafe()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.SafeToLog,
                    classDecoration: Decoration.None)).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsDecoratedAsSafe()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.SafeToLog)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("char")]
        [InlineData("short")]
        [InlineData("int")]
        [InlineData("long")]
        [InlineData("ushort")]
        [InlineData("uint")]
        [InlineData("ulong")]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        [InlineData("DateTime")]
        [InlineData("IntPtr")]
        [InlineData("UIntPtr")]
        [InlineData("TimeSpan")]
        [InlineData("DateTimeOffset")]
        [InlineData("Guid")]
        [InlineData("Uri")]
        [InlineData("System.Text.Encoding")]
        [InlineData("Type")]
        [InlineData("TypeCode")]
        public static async Task AnalyzeWhenSpecificExtendedPropertyTypeIsDecoratedAsSafe(string extendedPropertyType)
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: extendedPropertyType,
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None)).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyBaseTypeHasPropertyDecoratedAsSafe()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClassDerived",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.SafeToLog,
                    classDecoration: Decoration.None) + @"

public class TestClassDerived : TestClass
{
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsDecoratedAsSafeAndPropertiesAreDefinedInBaseType()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClassDerived",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None) + @"

[SafeToLog]
public class TestClassDerived : TestClass
{
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsDecoratedAsSafeAtRuntimeWithGenericMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None) + @"

public class Program
{
    public static void Main(string[] args)
    {
        SafeToLogAttribute.Decorate<TestClass>();
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeIsDecoratedAsSafeAtRuntimeWithNonGenericMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None) + @"

public class Program
{
    public static void Main(string[] args)
    {
        SafeToLogAttribute.Decorate(typeof(TestClass));
    }
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenExtendedPropertyTypeHasPropertyDecoratedAsSafeAtRuntime()
        {
            await TestAssistants.VerifyAnalyzerAsync<ExtendedPropertyNotMarkedSafeToLogAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: false,
                    propertyDecoration: Decoration.None,
                    classDecoration: Decoration.None) + @"

public class Program
{
    public static void Main(string[] args)
    {
        SafeToLogAttribute.Decorate<TestClass>(testClass => testClass.ExampleProperty);
    }
}").ConfigureAwait(false);
        }

        private static string GetTestCode(string extendedPropertyType, bool shouldReportDiagnostic,
            Decoration propertyDecoration, Decoration classDecoration)
        {
            var openDiagnostic = shouldReportDiagnostic ? "[|" : null;
            var closeDiagnostic = shouldReportDiagnostic ? "|]" : null;
            var propertyDecorationValue = propertyDecoration != Decoration.None ? $"[{propertyDecoration}]" : null;
            var classDecorationValue = classDecoration != Decoration.None ? $"[{classDecoration}]" : null;

            return string.Format(CultureInfo.InvariantCulture, @"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;

{4}
public class TestClass
{{
    {3}
    public string ExampleProperty {{ get; set; }}

    public void Call_LogEntry_SetSanitizedExtendedProperty(
        {0} exampleValue,
        LogEntry logEntry)
    {{
        // Call logEntry.SetSanitizedExtendedProperty
        logEntry.SetSanitizedExtendedProperty(""example"", {1}exampleValue{2});
    }}

    public void Call_Logging_ExtensionMethod_With_ExtendedProperties_Parameter(
        {0} exampleValue,
        ILogger logger,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod1,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod2,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod3,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod4,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod5,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod6,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod7,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod8,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod9,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod10,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod11,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod12,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod1,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod2,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod3,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod4,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod5,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod6,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod7,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod8,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod9,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod10,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod11,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod12,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer1,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer2,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer3,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer4,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer5,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer6,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer7,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer8,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer9,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer10,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer11,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer12)
    {{
        propertiesDictionaryParameterPopulatedWithAddMethod1.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod2.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod3.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod4.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod5.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod6.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod7.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod8.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod9.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod10.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod11.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithAddMethod12.Add(""example"", {1}exampleValue{2});

        propertiesDictionaryParameterPopulatedWithTryAddMethod1.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod2.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod3.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod4.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod5.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod6.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod7.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod8.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod9.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod10.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod11.TryAdd(""example"", {1}exampleValue{2});
        propertiesDictionaryParameterPopulatedWithTryAddMethod12.TryAdd(""example"", {1}exampleValue{2});

        propertiesDictionaryParameterPopulatedWithIndexer1[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer2[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer3[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer4[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer5[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer6[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer7[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer8[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer9[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer10[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer11[""example""] = {1}exampleValue{2};
        propertiesDictionaryParameterPopulatedWithIndexer12[""example""] = {1}exampleValue{2};

        var propertiesDictionaryVariableInitializedWithAddMethodInitializer1 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer2 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer3 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer4 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer5 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer6 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer7 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer8 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer9 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer10 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer11 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};
        var propertiesDictionaryVariableInitializedWithAddMethodInitializer12 = new Dictionary<string, object> {{ {{ ""example"", {1}exampleValue{2} }} }};

        var propertiesDictionaryVariableInitializedWithIndexerInitializer1 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer2 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer3 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer4 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer5 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer6 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer7 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer8 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer9 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer10 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer11 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};
        var propertiesDictionaryVariableInitializedWithIndexerInitializer12 = new Dictionary<string, object> {{ [""example""] = {1}exampleValue{2} }};

        var propertiesDictionaryVariableInitializedWithAddMethod1 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod2 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod3 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod4 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod5 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod6 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod7 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod8 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod9 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod10 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod11 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithAddMethod12 = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithAddMethod1.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod2.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod3.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod4.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod5.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod6.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod7.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod8.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod9.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod10.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod11.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithAddMethod12.Add(""example"", {1}exampleValue{2});

        var propertiesDictionaryVariableInitializedWithIndexer1 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer2 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer3 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer4 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer5 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer6 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer7 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer8 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer9 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer10 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer11 = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer12 = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithIndexer1[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer2[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer3[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer4[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer5[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer6[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer7[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer8[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer9[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer10[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer11[""example""] = {1}exampleValue{2};
        propertiesDictionaryVariableInitializedWithIndexer12[""example""] = {1}exampleValue{2};

        var propertiesAnonymousObject1 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject2 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject3 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject4 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject5 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject6 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject7 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject8 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject9 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject10 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject11 = new {{ example = {1}exampleValue{2} }};
        var propertiesAnonymousObject12 = new {{ example = {1}exampleValue{2} }};

        var exception = new Exception();

        // Call logging extension method when extendedProperties is in-line anonymous object
        logger.DebugSanitized(""Example message"", new {{ example = {1}exampleValue{2} }});
        logger.DebugSanitized(""Example message"", exception, new {{ example = {1}exampleValue{2} }});
        logger.InfoSanitized(""Example message"", new {{ example = {1}exampleValue{2} }});
        logger.InfoSanitized(""Example message"", exception, new {{ example = {1}exampleValue{2} }});
        logger.WarnSanitized(""Example message"", new {{ example = {1}exampleValue{2} }});
        logger.WarnSanitized(""Example message"", exception, new {{ example = {1}exampleValue{2} }});
        logger.ErrorSanitized(""Example message"", new {{ example = {1}exampleValue{2} }});
        logger.ErrorSanitized(""Example message"", exception, new {{ example = {1}exampleValue{2} }});
        logger.FatalSanitized(""Example message"", new {{ example = {1}exampleValue{2} }});
        logger.FatalSanitized(""Example message"", exception, new {{ example = {1}exampleValue{2} }});
        logger.AuditSanitized(""Example message"", new {{ example = {1}exampleValue{2} }});
        logger.AuditSanitized(""Example message"", exception, new {{ example = {1}exampleValue{2} }});

        // Call logging extension method when extendedProperties is anonymous object variable
        logger.DebugSanitized(""Example message"", propertiesAnonymousObject1);
        logger.DebugSanitized(""Example message"", exception, propertiesAnonymousObject2);
        logger.InfoSanitized(""Example message"", propertiesAnonymousObject3);
        logger.InfoSanitized(""Example message"", exception, propertiesAnonymousObject4);
        logger.WarnSanitized(""Example message"", propertiesAnonymousObject5);
        logger.WarnSanitized(""Example message"", exception, propertiesAnonymousObject6);
        logger.ErrorSanitized(""Example message"", propertiesAnonymousObject7);
        logger.ErrorSanitized(""Example message"", exception, propertiesAnonymousObject8);
        logger.FatalSanitized(""Example message"", propertiesAnonymousObject9);
        logger.FatalSanitized(""Example message"", exception, propertiesAnonymousObject10);
        logger.AuditSanitized(""Example message"", propertiesAnonymousObject11);
        logger.AuditSanitized(""Example message"", exception, propertiesAnonymousObject12);

        // Call logging extension method when extendedProperties is dictionary variable populated with indexer
        logger.DebugSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexer1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexer3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexer5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexer7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexer9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexer11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer12);

        // Call logging extension method when extendedProperties is dictionary variable populated with Add method
        logger.DebugSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod12);

        // Call logging extension method when extendedProperties is dictionary created with indexer initializer
        logger.DebugSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer12);

        // Call logging extension method when extendedProperties is dictionary created with Add method initializer
        logger.DebugSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer12);

        // Call logging extension method when extendedProperties is dictionary parameter populated with indexer
        logger.DebugSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer12);

        // Call logging extension method when extendedProperties is dictionary parameter populated with TryAdd method
        logger.DebugSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod12);

        // Call logging extension method when extendedProperties is dictionary parameter populated with Add method
        logger.DebugSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod1);
        logger.DebugSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod2);
        logger.InfoSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod3);
        logger.InfoSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod4);
        logger.WarnSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod5);
        logger.WarnSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod6);
        logger.ErrorSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod7);
        logger.ErrorSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod8);
        logger.FatalSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod9);
        logger.FatalSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod10);
        logger.AuditSanitized(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod11);
        logger.AuditSanitized(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod12);
    }}

    public void Call_LogEntry_SetSanitizedExtendedProperties(
        {0} exampleValue,
        LogEntry logEntry,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer)
    {{
        propertiesDictionaryParameterPopulatedWithAddMethod.Add(""example"", {1}exampleValue{2});

        propertiesDictionaryParameterPopulatedWithTryAddMethod.TryAdd(""example"", {1}exampleValue{2});

        propertiesDictionaryParameterPopulatedWithIndexer[""example""] = {1}exampleValue{2};

        var propertiesDictionaryVariableInitializedWithAddMethodInitializer = new Dictionary<string, object>
        {{
            {{ ""example"", {1}exampleValue{2} }}
        }};

        var propertiesDictionaryVariableInitializedWithIndexerInitializer = new Dictionary<string, object>
        {{
            [""example""] = {1}exampleValue{2}
        }};

        var propertiesDictionaryVariableInitializedWithAddMethod = new Dictionary<string, object>();
        var propertiesDictionaryVariableInitializedWithIndexer = new Dictionary<string, object>();

        propertiesDictionaryVariableInitializedWithAddMethod.Add(""example"", {1}exampleValue{2});
        propertiesDictionaryVariableInitializedWithIndexer[""example""] = {1}exampleValue{2};

        var propertiesAnonymousObject = new {{ example = {1}exampleValue{2} }};

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is in-line anonymous object
        logEntry.SetSanitizedExtendedProperties(new {{ example = {1}exampleValue{2} }});

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is anonymous object variable
        logEntry.SetSanitizedExtendedProperties(propertiesAnonymousObject);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary variable populated with indexer
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryVariableInitializedWithIndexer);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary variable populated with Add method
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryVariableInitializedWithAddMethod);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary created with indexer initializer
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryVariableInitializedWithIndexerInitializer);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary created with Add method initializer
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryVariableInitializedWithAddMethodInitializer);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary parameter populated with indexer
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryParameterPopulatedWithIndexer);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary parameter populated with TryAdd method
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryParameterPopulatedWithTryAddMethod);

        // Call logEntry.SetSanitizedExtendedProperties when extendedProperties is dictionary parameter populated with Add method
        logEntry.SetSanitizedExtendedProperties(propertiesDictionaryParameterPopulatedWithAddMethod);
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
#endif