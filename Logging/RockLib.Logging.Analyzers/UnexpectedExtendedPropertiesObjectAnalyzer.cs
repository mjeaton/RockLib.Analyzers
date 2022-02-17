using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnexpectedExtendedPropertiesObjectAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Unexpected extended properties object";
        private static readonly LocalizableString _messageFormat = "Unexpected type '{0}' used for extended properties";
        private static readonly LocalizableString _description = "An anonymous object or a string dictionary should be used as the value for extended properties.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
             DiagnosticIds.UnexpectedExtendedPropertiesObject,
             _title,
             _messageFormat,
             DiagnosticCategory.Usage,
             DiagnosticSeverity.Warning,
             isEnabledByDefault: true,
             description: _description,
             helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.UnexpectedExtendedPropertiesObject));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null) { throw new ArgumentNullException(nameof(context)); }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
            if (loggingExtensionsType is null) { return; }

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType is null) { return; }

            var loggerType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.ILogger");
            if (loggerType is null) { return; }

            var logEntryType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
            if (logEntryType is null) { return; }

            var analyzer = new OperationAnalyzer(loggerType, logEntryType, loggingExtensionsType, safeLoggingExtensionsType);

            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(analyzer.AnalyzeObjectCreation, OperationKind.ObjectCreation);
        }

        private sealed class OperationAnalyzer
        {
            private readonly INamedTypeSymbol _loggerType;
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _loggingExtensionsType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;

            public OperationAnalyzer(INamedTypeSymbol loggerType,
                INamedTypeSymbol logEntryType,
                INamedTypeSymbol loggingExtensionsType,
                INamedTypeSymbol safeLoggingExtensionsType)
            {
                _loggerType = loggerType;
                _logEntryType = logEntryType;
                _loggingExtensionsType = loggingExtensionsType;
                _safeLoggingExtensionsType = safeLoggingExtensionsType;
            }

            public void AnalyzeObjectCreation(OperationAnalysisContext context)
            {
                var objectCreationOperation = (IObjectCreationOperation)context.Operation;
                if (SymbolEqualityComparer.Default.Equals(objectCreationOperation.Type, _logEntryType))
                {
                    AnalyzeExtendedPropertiesArgument(objectCreationOperation.Arguments, context.ReportDiagnostic, objectCreationOperation.Syntax);
                }
            }

            public void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                    return;

                if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggerType)
                        || SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggingExtensionsType)
                        || SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _safeLoggingExtensionsType))
                {
                    AnalyzeExtendedPropertiesArgument(invocationOperation.Arguments, context.ReportDiagnostic, invocationOperation.Syntax);
                }
            }

            private static void AnalyzeExtendedPropertiesArgument(IEnumerable<IArgumentOperation> arguments,
                 Action<Diagnostic> reportDiagnostic, SyntaxNode reportingNode)
            {
                var extendedPropertiesArgument = arguments.FirstOrDefault(argument => argument.Parameter!.Name == "extendedProperties");

                if (extendedPropertiesArgument is null
                    || !(extendedPropertiesArgument.Value is IConversionOperation convertToObjectType)
                    || convertToObjectType.Type!.SpecialType != SpecialType.System_Object)
                    return;

                var extendedPropertiesArgumentValue = convertToObjectType.Operand;

                if (!extendedPropertiesArgumentValue.Type!.IsAnonymousType
                    && !IsValidDictionary(extendedPropertiesArgumentValue.Type))
                {
                    var diagnostic = Diagnostic.Create(Rule, reportingNode.GetLocation());
                    reportDiagnostic(diagnostic);
                }
            }

            private static bool IsValidDictionary(ITypeSymbol type)
            {
                // TODO: Reduce false positives (we should only match if it implements IEnumerable<KeyValuePair<string, T>> or IDictionary)
                return type
                    .AllInterfaces
                    .Any(x => x.MetadataName == typeof(IDictionary).Name);
            }
        }
    }
}
