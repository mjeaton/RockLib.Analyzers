﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.Analyzers.Test.CSharpAnalyzerVerifier<
	 RockLib.Logging.Analyzers.AnonymousObjectAnalyzer>;

namespace RockLib.Logging.Analyzers.Test
{
	[TestClass]
	public class UseAnonymousObjectAnalyzerTests
	{
		[TestMethod("Diagnostics are reported when logging with a non-anon type")]
		public async Task Should_report_anonymous_object_during_sanitized_log()
		{
			await RockLibVerifier.VerifyAnalyzerAsync(
				GetTestCode(
					extendedPropertyType: "ReportAnonObjectPreventionTestClass",					
					shouldReportDiagnostic: true));
		}

		private static string GetTestCode(string extendedPropertyType, bool shouldReportDiagnostic)
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
		{0}logger.DebugSanitized(""WE DA BEST MESSAGE"", new Derp(){{ Value = ""florp"" }}){1};
		{0}logger.WarnSanitized(""WE DA BEST MESSAGE"", new Derp(){{ Value = ""florp"" }}){1};
		{0}logger.InfoSanitized(""WE DA BEST MESSAGE"", new Derp(){{ Value = ""florp"" }}){1};
		{0}logger.ErrorSanitized(""WE DA BEST MESSAGE"", new Derp(){{ Value = ""florp"" }}){1};
	}}
}}", openDiagnostic, closeDiagnostic);
		}
	}
}
