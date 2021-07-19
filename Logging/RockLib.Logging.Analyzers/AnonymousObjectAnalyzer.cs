using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AnonymousObjectAnalyzer : DiagnosticAnalyzer
	{
		private static readonly LocalizableString _title = "Use anonymous object in sanitizing methods";
		private static readonly LocalizableString _messageFormat = "Use anonymous objects as extended property when calling sanitizing methods";
		private static readonly LocalizableString _description = "It is recommended to use anonymous objects when calling sanitizing methods.";

		public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			 DiagnosticIds.UseSanitizingLoggingMethod,
			 _title,
			 _messageFormat,
			 DiagnosticCategory.Usage,
			 DiagnosticSeverity.Warning,
			 isEnabledByDefault: true,
			 description: _description,
			 helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.UseAnonymousObject));

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(OnCompilationStart);
		}

		private static void OnCompilationStart(CompilationStartAnalysisContext context)
		{
			var logEntryType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
			if (logEntryType == null)
				return;

			var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
			if (loggingExtensionsType == null)
				return;

			var analyzer = new OperationAnalyzer(logEntryType, loggingExtensionsType);

			context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
		}

		private class OperationAnalyzer
		{
			private readonly INamedTypeSymbol _logEntryType;
			private readonly INamedTypeSymbol _loggingExtensionsType;

			public OperationAnalyzer(INamedTypeSymbol logEntryType, INamedTypeSymbol loggingExtensionsType)
			{
				_logEntryType = logEntryType;
				_loggingExtensionsType = loggingExtensionsType;
			}

			public void AnalyzeInvocation(OperationAnalysisContext context)
			{
				var invocationOperation = (IInvocationOperation)context.Operation;
				var methodSymbol = invocationOperation.TargetMethod;

				if (methodSymbol.MethodKind != MethodKind.Ordinary)
					return;

				if (methodSymbol.Name.Contains("Sanitized"))
				{
					AnalyzeExtendedPropertiesArgument(invocationOperation.Arguments, context.ReportDiagnostic,
				 invocationOperation.Syntax, "'ILogger." + methodSymbol.Name, "'ILogger." + methodSymbol.Name + "'");
				}			
			}

			private void AnalyzeExtendedPropertiesArgument(IEnumerable<IArgumentOperation> arguments,
				 Action<Diagnostic> reportDiagnostic, SyntaxNode reportingNode, string recommendedFormat, string notRecommendedFormat)
			{
				var extendedPropertiesArgument = arguments.FirstOrDefault(argument => argument.Parameter.Name == "extendedProperties");

				if (extendedPropertiesArgument == null
						  || !(extendedPropertiesArgument.Value is IConversionOperation convertToObjectType)
						  || convertToObjectType.Type.SpecialType != SpecialType.System_Object)
					return;

				var extendedPropertiesArgumentValue = convertToObjectType.Operand;

				if (!extendedPropertiesArgumentValue.Type.IsAnonymousType)
				{
					var diagnostic = Diagnostic.Create(Rule, reportingNode.GetLocation(),
						 recommendedFormat, notRecommendedFormat);
					reportDiagnostic(diagnostic);
				}
			}
		}
	}
}
