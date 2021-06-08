using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExtendedPropertyNotMarkedSafeToLogAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Extended property is not marked as safe to log";
        private static readonly LocalizableString _messageFormat = "The '{0}' type does not have any properties marked as safe to log";
        private static readonly LocalizableString _description = "The value of a sanitized extended property should have a type with one or more properties decorated with the [SafeToLog] or else be decorated with the [SafeToLog] attribute itself.";

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

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType == null)
                return;

            var safeToLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeToLogAttribute");
            if (safeToLogAttributeType == null)
                return;

            var notSafeToLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.NotSafeToLogAttribute");
            if (notSafeToLogAttributeType == null)
                return;

            var analyzer = new InvocationOperationAnalyzer(logEntryType, safeLoggingExtensionsType,
                safeToLogAttributeType, notSafeToLogAttributeType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;
            private readonly INamedTypeSymbol _safeToLogAttributeType;
            private readonly INamedTypeSymbol _notSafeToLogAttributeType;

            public InvocationOperationAnalyzer(INamedTypeSymbol logEntryType, INamedTypeSymbol safeLoggingExtensionsType,
                INamedTypeSymbol safeToLogAttributeType, INamedTypeSymbol notSafeToLogAttributeType)
            {
                _logEntryType = logEntryType;
                _safeLoggingExtensionsType = safeLoggingExtensionsType;
                _safeToLogAttributeType = safeToLogAttributeType;
                _notSafeToLogAttributeType = notSafeToLogAttributeType;
            }

            public void Analyze(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                    return;

                if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _safeLoggingExtensionsType))
                    AnalyzeExtendedPropertiesArgument(context, invocationOperation);
                else if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _logEntryType))
                    if (methodSymbol.Name == "SetSanitizedExtendedProperty")
                        AnalyzeSetSanitizedExtendedPropertyMethodCall(context, invocationOperation);
                    else if (methodSymbol.Name == "SetSanitizedExtendedProperties")
                        AnalyzeExtendedPropertiesArgument(context, invocationOperation);
            }

            private void AnalyzeSetSanitizedExtendedPropertyMethodCall(OperationAnalysisContext context,
                IInvocationOperation invocationOperation)
            {
                var valueArgument = invocationOperation.Arguments[1];
                if (valueArgument.Value is IConversionOperation convertToObjectType
                    && convertToObjectType.Type.SpecialType == SpecialType.System_Object)
                {
                    AnalyzePropertyValue(convertToObjectType.Operand, context.ReportDiagnostic);
                }
            }

            private void AnalyzeExtendedPropertiesArgument(OperationAnalysisContext context, IInvocationOperation invocationOperation)
            {
                var extendedPropertiesArgument = invocationOperation.Arguments
                        .FirstOrDefault(argument => argument.Parameter.Name == "extendedProperties");

                if (extendedPropertiesArgument == null
                    || !(extendedPropertiesArgument.Value is IConversionOperation convertToObjectType)
                    || convertToObjectType.Type.SpecialType != SpecialType.System_Object)
                {
                    return;
                }

                var extendedPropertiesArgumentValue = convertToObjectType.Operand;

                if (extendedPropertiesArgumentValue.TryGetAnonymousObjectCreationOperation(out var anonymousObjectCreationOperation))
                {
                    foreach (ISimpleAssignmentOperation initializer in anonymousObjectCreationOperation.Initializers)
                        AnalyzePropertyValue(initializer.Value, context.ReportDiagnostic);
                }
                else if (extendedPropertiesArgumentValue.TryGetDictionaryExtendedPropertyValueOperations(out var dictionaryExtendedPropertyValues))
                {
                    foreach (var extendedPropertyValue in dictionaryExtendedPropertyValues)
                        AnalyzePropertyValue(extendedPropertyValue, context.ReportDiagnostic);
                }
            }

            private void AnalyzePropertyValue(IOperation propertyValue, Action<Diagnostic> reportDiagnostic)
            {
                if (propertyValue.Type is null || propertyValue.Type.IsValueType())
                    return;

                if (IsDecoratedWithSafeToLogAttribute(propertyValue.Type))
                {
                    if (propertyValue.Type.GetPublicProperties().Any(IsNotDecoratedWithNotSafeToLogAttribute))
                        return;
                }
                else
                {
                    if (propertyValue.Type.GetPublicProperties().Any(IsDecoratedWithSafeToLogAttribute))
                        return;
                }

                // "The '{0}' type does not have any properties marked as safe to log"
                var diagnostic = Diagnostic.Create(Rule, propertyValue.Syntax.GetLocation(), propertyValue.Type);
                reportDiagnostic(diagnostic);
            }

            private bool IsDecoratedWithSafeToLogAttribute(ISymbol symbol) =>
                symbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _safeToLogAttributeType));

            private bool IsNotDecoratedWithNotSafeToLogAttribute(ISymbol symbol) =>
                !symbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _notSafeToLogAttributeType));
        }
    }
}
