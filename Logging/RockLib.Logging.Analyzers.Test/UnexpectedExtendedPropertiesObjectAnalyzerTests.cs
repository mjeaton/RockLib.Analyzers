using Xunit;
using System.Threading.Tasks;

namespace RockLib.Logging.Analyzers.Test
{
    public static class UnexpectedExtendedPropertiesObjectAnalyzerTests
    {
        [Fact]
        public static async Task AnalyzeWhenLoggingWithNonAnonymousType()
        {
            await TestAssistants.VerifyAnalyzerAsync<UnexpectedExtendedPropertiesObjectAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;
public class Derp
{
	public string Value { get; set; }
}

public class TestClass
{
	public void Warn_All(       
        ILogger logger)
	{
		[|logger.DebugSanitized(""Debug Message"", new Derp(){ Value = ""florp"" })|];
		[|logger.WarnSanitized(""Warn Message"", new Derp(){ Value = ""florp"" })|];
		[|logger.InfoSanitized(""Info Message"", new Derp(){ Value = ""florp"" })|];
		[|logger.ErrorSanitized(""Error Message"", new Derp(){ Value = ""florp"" })|];

		[|logger.Debug(""Debug Message"", new Derp(){ Value = ""florp"" })|];
		[|logger.Warn(""Warn Message"", new Derp(){ Value = ""florp"" })|];
		[|logger.Info(""Info Message"", new Derp(){ Value = ""florp"" })|];
		[|logger.Error(""Error Message"", new Derp(){ Value = ""florp"" })|];
	}
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenLoggingWithAnonymousType()
        {
            await TestAssistants.VerifyAnalyzerAsync<UnexpectedExtendedPropertiesObjectAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;

public class TestClass
{
	public void Do_Not_Warn(       
        ILogger logger)
	{
		logger.DebugSanitized(""Debug Message"", new { Value = ""florp""});
		logger.WarnSanitized(""Warn Message"", new { Value = ""florp""});
		logger.InfoSanitized(""Info Message"", new { Value = ""florp""});
		logger.ErrorSanitized(""Error Message"", new { Value = ""florp""});

		logger.Debug(""Debug Message"", new { Value = ""florp""});
		logger.Warn(""Warn Message"", new { Value = ""florp""});
		logger.Info(""Info Message"", new { Value = ""florp""});
		logger.Error(""Error Message"", new { Value = ""florp""});
		
		var dictionary = new Dictionary<string, object>();
		dictionary.Add(""glip"", ""glop"");
		logger.DebugSanitized(""DictionaryDebug Message"", dictionary);
	}
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenInitializingEntryWithNonAnonymousExtendedProperty()
        {
            await TestAssistants.VerifyAnalyzerAsync<UnexpectedExtendedPropertiesObjectAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;
public class Derp
{
	public string Value { get; set; }
}

public class TestClass
{
	public void Warn_LogEntry()
	{
		var entry = [|new LogEntry(""message 1"", extendedProperties: new Derp())|];
	}
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenInitializingEntryWithAnonymousExtendedProperty()
        {
            await TestAssistants.VerifyAnalyzerAsync<UnexpectedExtendedPropertiesObjectAnalyzer>(
@"using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;

public class TestClass
{
	public void Warn_LogEntry()
	{
		var entry = new LogEntry(""message 1"", extendedProperties: new { Flip = ""Florp"" });
		var entry2 = new LogEntry(""message 1"", extendedProperties: new Dictionary<string, string>());	
	}
}").ConfigureAwait(false);
        }
    }
}
