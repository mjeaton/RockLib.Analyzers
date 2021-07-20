using Xunit;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
	 RockLib.Logging.Analyzers.AnonymousObjectAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
	public class UseAnonymousObjectAnalyzerTests
	{
		[Fact(DisplayName = "Diagnostics are reported when logging with a non-anon type")]
		public async Task Should_report_anonymous_object_during_sanitized_log()
		{
			await RockLibVerifier.VerifyAnalyzerAsync(
				GetTestCode(					
					shouldReportDiagnostic: true));
		}

		private static string GetTestCode(bool shouldReportDiagnostic)
		{
			string openDiagnostic = shouldReportDiagnostic ? "[|" : null;
			string closeDiagnostic = shouldReportDiagnostic ? "|]" : null;

			return string.Format(@"
using RockLib.Logging;
using RockLib.Logging.SafeLogging;
using System;
using System.Collections.Generic;
public class Derp
{{
	public string Value {{ get; set; }}
}}

public class ReportAnonObjectPreventionTestClass
{{
	public void Log_Sanitized(       
        ILogger logger)
	{{
		{0}logger.DebugSanitized(""Debug Message"", new Derp(){{ Value = ""florp"" }}){1};
		{0}logger.WarnSanitized(""Warn Message"", new Derp(){{ Value = ""florp"" }}){1};
		{0}logger.InfoSanitized(""Info Message"", new Derp(){{ Value = ""florp"" }}){1};
		{0}logger.ErrorSanitized(""Error Message"", new Derp(){{ Value = ""florp"" }}){1};
	}}
}}", openDiagnostic, closeDiagnostic);
		}
	}
}
