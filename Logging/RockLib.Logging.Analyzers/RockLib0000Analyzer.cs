using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RockLib0000Analyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.RockLib0000);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(c =>
            {
                var logEntryType = c.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
                if (logEntryType == null)
                    return;

                var safeLoggingExtensionsType = c.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
                if (safeLoggingExtensionsType == null)
                    return;

                c.RegisterOperationAction(cx =>
                    AnalyzeInvocationOperation(cx, logEntryType, safeLoggingExtensionsType),
                    OperationKind.Invocation);
            });
        }

        private void AnalyzeInvocationOperation(OperationAnalysisContext context, INamedTypeSymbol logEntryType, INamedTypeSymbol safeLoggingExtensionsType)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var methodSymbol = invocationOperation.TargetMethod;

            if (methodSymbol.MethodKind != MethodKind.Ordinary)
                return;

            if (methodSymbol.Name == "SetSanitizedExtendedProperty"
                && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, logEntryType))
            {
                // TODO: Analyze SetSanitizedExtendedProperty method call
            }
            else if (methodSymbol.Name == "SetSanitizedExtendedProperties"
                && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, logEntryType))
            {
                // TODO: Analyze SetSanitizedExtendedProperties method call
            }
            else if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, safeLoggingExtensionsType))
            {
                // TODO: Analyze sanitizing logging extension method call
            }
        }
    }
}
