using Xunit;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
	 RockLib.Logging.Analyzers.AnonymousObjectAnalyzer>;


namespace RockLib.Logging.Analyzers.Test
{
	public class UseAnonymousObjectAnalyzerTests
	{
		[Fact(DisplayName = "Diagnostics are reported when logging with a non-anon type")]
		public async Task DiagnosticReported1()
		{
			await RockLibVerifier.VerifyAnalyzerAsync(
				@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;
public class Derp
{
	public string Value { get; set; }
}

public class ReportAnonObjectPreventionTestClass
{
	public void Log_Sanitized(       
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
}");
		}

		[Fact(DisplayName = "Diagnostics are not reported when logging with a anon type")]
		public async Task DiagnosticReported2()
		{			
			await RockLibVerifier.VerifyAnalyzerAsync(
				@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;
public class Derp
{
	public string Value { get; set; }
}

public class ReportAnonObjectPreventionTestClass
{
	public void Log_Sanitized(       
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
}");
		}
	}
}
