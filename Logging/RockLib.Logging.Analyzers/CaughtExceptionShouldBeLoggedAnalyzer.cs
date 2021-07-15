using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CaughtExceptionShouldBeLoggedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "The caught exception should be passed into the logging method";
        private static readonly LocalizableString _messageFormat = "The caught {0} exception should be passed into the logging method";
        private static readonly LocalizableString _description = "If a logging method is inside a catch block, the caught exception should be passed to it.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.ExtendedPropertyNotMarkedSafeToLog,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.ExtendedPropertyNotMarkedSafeToLog));

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

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType == null)
                return;

            var analyzer = new InvocationOperationAnalyzer(logEntryType, loggingExtensionsType, safeLoggingExtensionsType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _loggingExtensionsType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;

            public InvocationOperationAnalyzer(INamedTypeSymbol logEntryType, INamedTypeSymbol loggingExtensionsType,
                INamedTypeSymbol safeLoggingExtensionsType)
            {
                _logEntryType = logEntryType;
                _loggingExtensionsType = loggingExtensionsType;
                _safeLoggingExtensionsType = safeLoggingExtensionsType;
            }

            public void Analyze(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                //TODO: LogEntry initializer?
                if (methodSymbol.MethodKind == MethodKind.Constructor)
                {
                    //LogEntry constructor
                }
                else if (methodSymbol.MethodKind == MethodKind.Ordinary)
                {
                    var containingType = methodSymbol.ContainingType;
                    if (!SymbolEqualityComparer.Default.Equals(containingType, _loggingExtensionsType) 
                        && !SymbolEqualityComparer.Default.Equals(containingType, _safeLoggingExtensionsType))
                        return;

                    if (ContainsExceptionParameter(invocationOperation))
                        return;

                    if (!IsInCatchBlock(invocationOperation))
                        return;

                    var diagnostic = Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private bool IsInCatchBlock(IInvocationOperation invocationOperation)
            {
                var parent = invocationOperation.Parent;
                while (parent != null)
                {
                    //TODO: Other catch operations?
                    if (parent is ICatchClauseOperation)
                        return true;
                    parent = parent.Parent;
                }
                return false;
            }

            private bool ContainsExceptionParameter(IInvocationOperation invocationOperation)
            {
                var argument = invocationOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "exception");
                if (argument == null || argument.IsImplicit)
                    return false;
                return true;
            }
        }
    }
}
