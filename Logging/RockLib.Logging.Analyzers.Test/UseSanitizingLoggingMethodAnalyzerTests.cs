#if !NET48
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.Analyzers.Test
{
    public static class UseSanitizingLoggingMethodAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenSettingExtendedPropertyWithNonValueType()
        {
            await TestAssistants.VerifyAnalyzerAsync<UseSanitizingLoggingMethodAnalyzer>(
                GetTestCode(
                    extendedPropertyType: "TestClass",
                    shouldReportDiagnostic: true)).ConfigureAwait(false);
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
        public static async Task AnalyzextendedPropertiesOfSpecifiedType(string extendedPropertyType)
        {
            await TestAssistants.VerifyAnalyzerAsync<UseSanitizingLoggingMethodAnalyzer>(
                GetTestCode(
                    extendedPropertyType: extendedPropertyType,
                    shouldReportDiagnostic: false)).ConfigureAwait(false);
        }


        private static string GetTestCode(string extendedPropertyType, bool shouldReportDiagnostic)
        {
            var openDiagnostic = shouldReportDiagnostic ? "[|" : null;
            var closeDiagnostic = shouldReportDiagnostic ? "|]" : null;

            return string.Format(CultureInfo.InvariantCulture, @"
using RockLib.Logging;
using System;
using System.Collections.Generic;

public class TestClass
{{
    public void Set_LogEntry_ExtendedProperty_Value(
        {0} exampleValue,
        LogEntry logEntry)
    {{
        // Set LogEntry.ExtendedProperty value with indexer
        {1}logEntry.ExtendedProperties[""example""] = exampleValue{2};

        // Set LogEntry.ExtendedProperty value with Add method
        {1}logEntry.ExtendedProperties.Add(""example"", exampleValue){2};

        // Set LogEntry.ExtendedProperty value with TryAdd method
        {1}logEntry.ExtendedProperties.TryAdd(""example"", exampleValue){2};
    }}

    public void Call_Logging_ExtensionMethod_With_ExtendedProperties_Parameter(
        {0} exampleValue,
        ILogger logger,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer)
    {{
        propertiesDictionaryParameterPopulatedWithAddMethod.Add(""example"", exampleValue);

        propertiesDictionaryParameterPopulatedWithTryAddMethod.TryAdd(""example"", exampleValue);

        propertiesDictionaryParameterPopulatedWithIndexer[""example""] = exampleValue;

        var propertiesDictionaryVariableInitializedWithAddMethodInitializer = new Dictionary<string, object>
        {{
            {{ ""example"", exampleValue }}
        }};

        var propertiesDictionaryVariableInitializedWithIndexerInitializer = new Dictionary<string, object>
        {{
            [""example""] = exampleValue
        }};

        var propertiesDictionaryVariableInitializedWithAddMethod = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithAddMethod.Add(""example"", exampleValue);

        var propertiesDictionaryVariableInitializedWithIndexer = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithIndexer[""example""] = exampleValue;

        var propertiesAnonymousObject = new {{ example = exampleValue }};

        var exception = new Exception();

        // Call logging extension method when extendedProperties is in-line anonymous object
        {1}logger.Debug(""Example message"", new {{ example = exampleValue }}){2};
        {1}logger.Debug(""Example message"", exception, new {{ example = exampleValue }}){2};
        {1}logger.Info(""Example message"", new {{ example = exampleValue }}){2};
        {1}logger.Info(""Example message"", exception, new {{ example = exampleValue }}){2};
        {1}logger.Warn(""Example message"", new {{ example = exampleValue }}){2};
        {1}logger.Warn(""Example message"", exception, new {{ example = exampleValue }}){2};
        {1}logger.Error(""Example message"", new {{ example = exampleValue }}){2};
        {1}logger.Error(""Example message"", exception, new {{ example = exampleValue }}){2};
        {1}logger.Fatal(""Example message"", new {{ example = exampleValue }}){2};
        {1}logger.Fatal(""Example message"", exception, new {{ example = exampleValue }}){2};
        {1}logger.Audit(""Example message"", new {{ example = exampleValue }}){2};
        {1}logger.Audit(""Example message"", exception, new {{ example = exampleValue }}){2};

        // Call logging extension method when extendedProperties is anonymous object variable
        {1}logger.Debug(""Example message"", propertiesAnonymousObject){2};
        {1}logger.Debug(""Example message"", exception, propertiesAnonymousObject){2};
        {1}logger.Info(""Example message"", propertiesAnonymousObject){2};
        {1}logger.Info(""Example message"", exception, propertiesAnonymousObject){2};
        {1}logger.Warn(""Example message"", propertiesAnonymousObject){2};
        {1}logger.Warn(""Example message"", exception, propertiesAnonymousObject){2};
        {1}logger.Error(""Example message"", propertiesAnonymousObject){2};
        {1}logger.Error(""Example message"", exception, propertiesAnonymousObject){2};
        {1}logger.Fatal(""Example message"", propertiesAnonymousObject){2};
        {1}logger.Fatal(""Example message"", exception, propertiesAnonymousObject){2};
        {1}logger.Audit(""Example message"", propertiesAnonymousObject){2};
        {1}logger.Audit(""Example message"", exception, propertiesAnonymousObject){2};

        // Call logging extension method when extendedProperties is dictionary variable populated with indexer
        {1}logger.Debug(""Example message"", propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Info(""Example message"", propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Error(""Example message"", propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryVariableInitializedWithIndexer){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexer){2};

        // Call logging extension method when extendedProperties is dictionary variable populated with Add method
        {1}logger.Debug(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Info(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Error(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryVariableInitializedWithAddMethod){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethod){2};

        // Call logging extension method when extendedProperties is dictionary created with indexer initializer
        {1}logger.Debug(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Info(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Error(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryVariableInitializedWithIndexerInitializer){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryVariableInitializedWithIndexerInitializer){2};

        // Call logging extension method when extendedProperties is dictionary created with Add method initializer
        {1}logger.Debug(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Info(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Error(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};

        // Call logging extension method when extendedProperties is dictionary parameter populated with indexer
        {1}logger.Debug(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Info(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Error(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryParameterPopulatedWithIndexer){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryParameterPopulatedWithIndexer){2};

        // Call logging extension method when extendedProperties is dictionary parameter populated with TryAdd method
        {1}logger.Debug(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Info(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Error(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryParameterPopulatedWithTryAddMethod){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryParameterPopulatedWithTryAddMethod){2};

        // Call logging extension method when extendedProperties is dictionary parameter populated with Add method
        {1}logger.Debug(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Debug(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Info(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Info(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Warn(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Warn(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Error(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Error(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Fatal(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Fatal(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Audit(""Example message"", propertiesDictionaryParameterPopulatedWithAddMethod){2};
        {1}logger.Audit(""Example message"", exception, propertiesDictionaryParameterPopulatedWithAddMethod){2};
    }}

    public void Call_LogEntry_Constructor_With_ExtendedProperties_Parameter(
        {0} exampleValue,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer)
    {{
        propertiesDictionaryParameterPopulatedWithAddMethod.Add(""example"", exampleValue);

        propertiesDictionaryParameterPopulatedWithTryAddMethod.TryAdd(""example"", exampleValue);

        propertiesDictionaryParameterPopulatedWithIndexer[""example""] = exampleValue;

        var propertiesDictionaryVariableInitializedWithAddMethodInitializer = new Dictionary<string, object>
        {{
            {{ ""example"", exampleValue }}
        }};

        var propertiesDictionaryVariableInitializedWithIndexerInitializer = new Dictionary<string, object>
        {{
            [""example""] = exampleValue
        }};

        var propertiesDictionaryVariableInitializedWithAddMethod = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithAddMethod.Add(""example"", exampleValue);

        var propertiesDictionaryVariableInitializedWithIndexer = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithIndexer[""example""] = exampleValue;

        var propertiesAnonymousObject = new {{ example = exampleValue }};

        // Call LogEntry constructor when extendedProperties is in-line anonymous object
        {1}new LogEntry(""Example message"", extendedProperties: new {{ example = exampleValue }}){2};

        // Call LogEntry constructor when extendedProperties is anonymous object variable
        {1}new LogEntry(""Example message"", extendedProperties: propertiesAnonymousObject){2};

        // Call LogEntry constructor when extendedProperties is dictionary variable populated with indexer
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryVariableInitializedWithIndexer){2};

        // Call LogEntry constructor when extendedProperties is dictionary variable populated with Add method
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryVariableInitializedWithAddMethod){2};

        // Call LogEntry constructor when extendedProperties is dictionary created with indexer initializer
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryVariableInitializedWithIndexerInitializer){2};

        // Call LogEntry constructor when extendedProperties is dictionary created with Add method initializer
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};

        // Call LogEntry constructor when extendedProperties is dictionary parameter populated with indexer
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryParameterPopulatedWithIndexer){2};

        // Call LogEntry constructor when extendedProperties is dictionary parameter populated with TryAdd method
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryParameterPopulatedWithTryAddMethod){2};

        // Call LogEntry constructor when extendedProperties is dictionary parameter populated with Add method
        {1}new LogEntry(""Example message"", extendedProperties: propertiesDictionaryParameterPopulatedWithAddMethod){2};
    }}

    public void Call_LogEntry_SetExtendedProperties(
        {0} exampleValue,
        LogEntry logEntry,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithTryAddMethod,
        IDictionary<string, object> propertiesDictionaryParameterPopulatedWithIndexer)
    {{
        propertiesDictionaryParameterPopulatedWithAddMethod.Add(""example"", exampleValue);

        propertiesDictionaryParameterPopulatedWithTryAddMethod.TryAdd(""example"", exampleValue);

        propertiesDictionaryParameterPopulatedWithIndexer[""example""] = exampleValue;

        var propertiesDictionaryVariableInitializedWithAddMethodInitializer = new Dictionary<string, object>
        {{
            {{ ""example"", exampleValue }}
        }};

        var propertiesDictionaryVariableInitializedWithIndexerInitializer = new Dictionary<string, object>
        {{
            [""example""] = exampleValue
        }};

        var propertiesDictionaryVariableInitializedWithAddMethod = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithAddMethod.Add(""example"", exampleValue);

        var propertiesDictionaryVariableInitializedWithIndexer = new Dictionary<string, object>();
        propertiesDictionaryVariableInitializedWithIndexer[""example""] = exampleValue;

        var propertiesAnonymousObject = new {{ example = exampleValue }};

        // Call logEntry.SetExtendedProperties when extendedProperties is in-line anonymous object
        {1}logEntry.SetExtendedProperties(new {{ example = exampleValue }}){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is anonymous object variable
        {1}logEntry.SetExtendedProperties(propertiesAnonymousObject){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary variable populated with indexer
        {1}logEntry.SetExtendedProperties(propertiesDictionaryVariableInitializedWithIndexer){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary variable populated with Add method
        {1}logEntry.SetExtendedProperties(propertiesDictionaryVariableInitializedWithAddMethod){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary created with indexer initializer
        {1}logEntry.SetExtendedProperties(propertiesDictionaryVariableInitializedWithIndexerInitializer){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary created with Add method initializer
        {1}logEntry.SetExtendedProperties(propertiesDictionaryVariableInitializedWithAddMethodInitializer){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary parameter populated with indexer
        {1}logEntry.SetExtendedProperties(propertiesDictionaryParameterPopulatedWithIndexer){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary parameter populated with TryAdd method
        {1}logEntry.SetExtendedProperties(propertiesDictionaryParameterPopulatedWithTryAddMethod){2};

        // Call logEntry.SetExtendedProperties when extendedProperties is dictionary parameter populated with Add method
        {1}logEntry.SetExtendedProperties(propertiesDictionaryParameterPopulatedWithAddMethod){2};
    }}
}}", extendedPropertyType, openDiagnostic, closeDiagnostic);
        }
    }
}
#endif