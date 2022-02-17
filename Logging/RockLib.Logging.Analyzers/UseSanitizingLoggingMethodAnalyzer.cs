using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseSanitizingLoggingMethodAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Use sanitizing logging method";
        private static readonly LocalizableString _messageFormat = "Call {0} instead of {1} in order to sanitize extended properties";
        private static readonly LocalizableString _description = "It is recommended to use sanitizing logging methods when adding extended properties with complex types.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.UseSanitizingLoggingMethod,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.UseSanitizingLoggingMethod));

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
            var logEntryType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
            if (logEntryType is null) { return; }

            var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
            if (loggingExtensionsType is null) { return; }

            var analyzer = new OperationAnalyzer(logEntryType, loggingExtensionsType);

            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(analyzer.AnalyzeAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(analyzer.AnalyzeObjectCreation, OperationKind.ObjectCreation);
        }

        private sealed class OperationAnalyzer
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

                if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggingExtensionsType))
                {
                    AnalyzeExtendedPropertiesArgument(invocationOperation.Arguments, context.ReportDiagnostic,
                        invocationOperation.Syntax, "'ILogger." + methodSymbol.Name + "Sanitized'", "'ILogger." + methodSymbol.Name + "'");
                }
                else if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _logEntryType)
                    && methodSymbol.Name == "SetExtendedProperties")
                {
                    AnalyzeExtendedPropertiesArgument(invocationOperation.Arguments, context.ReportDiagnostic,
                        invocationOperation.Syntax, "'LogEntry.SetSanitizedExtendedProperties'", "'LogEntry.SetExtendedProperties'");
                }
                else if ((methodSymbol.Name == "Add" || methodSymbol.Name == "TryAdd")
                    && invocationOperation.Instance is IPropertyReferenceOperation property
                    && property.Member.Name == "ExtendedProperties"
                    && SymbolEqualityComparer.Default.Equals(property.Instance!.Type, _logEntryType)
                    && invocationOperation.Arguments[1].Value is IConversionOperation conversion
                    && !conversion.Operand.Type!.IsValueType())
                {
                    var diagnostic = Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation(),
                        "'LogEntry.SetSanitizedExtendedProperty'", "'LogEntry.ExtendedProperties." + methodSymbol.Name + "'");
                    context.ReportDiagnostic(diagnostic);
                }
            }

            public void AnalyzeAssignment(OperationAnalysisContext context)
            {
                var assignmentOperation = (ISimpleAssignmentOperation)context.Operation;

                if (assignmentOperation.Target is IPropertyReferenceOperation indexerOperation
                    && indexerOperation.Arguments.Length == 1
                    && indexerOperation.Arguments[0].Value.Type!.SpecialType == SpecialType.System_String
                    && indexerOperation.Instance is IPropertyReferenceOperation extendedPropertiesOperation
                    && extendedPropertiesOperation.Arguments.Length == 0
                    && extendedPropertiesOperation.Property.Name == "ExtendedProperties"
                    && SymbolEqualityComparer.Default.Equals(extendedPropertiesOperation.Property.ContainingType, _logEntryType)
                    && assignmentOperation.Value is IConversionOperation conversion
                    && !conversion.Operand.Type!.IsValueType())
                {
                    var diagnostic = Diagnostic.Create(Rule, assignmentOperation.Syntax.GetLocation(),
                        "'LogEntry.SetSanitizedExtendedProperty'", "assigning to the 'LogEntry.ExtendedProperties' indexer");
                    context.ReportDiagnostic(diagnostic);
                }
            }

            public void AnalyzeObjectCreation(OperationAnalysisContext context)
            {
                var objectCreationOperation = (IObjectCreationOperation)context.Operation;
                if (SymbolEqualityComparer.Default.Equals(objectCreationOperation.Type, _logEntryType))
                {
                    AnalyzeExtendedPropertiesArgument(objectCreationOperation.Arguments, context.ReportDiagnostic,
                        objectCreationOperation.Syntax, "'LogEntry.SetSanitizedExtendedProperties'", "passing an extendedProperties argument to the 'LogEntry' constructor");
                }
            }

            private static void AnalyzeExtendedPropertiesArgument(IEnumerable<IArgumentOperation> arguments,
                Action<Diagnostic> reportDiagnostic, SyntaxNode reportingNode, string recommendedFormat, string notRecommendedFormat)
            {
                var extendedPropertiesArgument = arguments.FirstOrDefault(argument => argument.Parameter!.Name == "extendedProperties");

                if (extendedPropertiesArgument is null
                    || extendedPropertiesArgument.Value is not IConversionOperation convertToObjectType
                    || convertToObjectType.Type!.SpecialType != SpecialType.System_Object)
                    return;

                var extendedPropertiesArgumentValue = convertToObjectType.Operand;

                if ((extendedPropertiesArgumentValue.TryGetAnonymousObjectCreationOperation(out var anonymousObjectCreationOperation)
                        && anonymousObjectCreationOperation.Initializers.Any(initializer => !((ISimpleAssignmentOperation)initializer).Value.Type!.IsValueType()))
                    || (extendedPropertiesArgumentValue.TryGetDictionaryExtendedPropertyValueOperations(out var dictionaryExtendedPropertyValues)
                        && dictionaryExtendedPropertyValues.Any(value => !value?.Type!.IsValueType() ?? false)))
                {
                    var diagnostic = Diagnostic.Create(Rule, reportingNode.GetLocation(),
                        recommendedFormat, notRecommendedFormat);
                    reportDiagnostic(diagnostic);
                }
            }
        }
    }
}
