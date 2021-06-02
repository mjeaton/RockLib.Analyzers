using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
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

        public const string DiagnosticId = DiagnosticIds.ExtendedPropertyNotMarkedSafeToLogRuleId;

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var objectType = context.Compilation.GetTypeByMetadataName("System.Object");
            if (objectType == null)
                return;

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

            var analyzer = new InvocationOperationAnalyzer(objectType, logEntryType,
                safeLoggingExtensionsType, safeToLogAttributeType, notSafeToLogAttributeType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _objectType;
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;
            private readonly INamedTypeSymbol _safeToLogAttributeType;
            private readonly INamedTypeSymbol _notSafeToLogAttributeType;

            public InvocationOperationAnalyzer(INamedTypeSymbol objectType, INamedTypeSymbol logEntryType,
                INamedTypeSymbol safeLoggingExtensionsType, INamedTypeSymbol safeToLogAttributeType, INamedTypeSymbol notSafeToLogAttributeType)
            {
                _objectType = objectType;
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
                    && SymbolEqualityComparer.Default.Equals(convertToObjectType.Type, _objectType))
                {
                    AnalyzePropertyValue(context, convertToObjectType.Operand);
                }
                else
                {
                    // TODO: Is it worth looking for the type of the value anywhere else?
                }
            }

            private void AnalyzeExtendedPropertiesArgument(OperationAnalysisContext context, IInvocationOperation invocationOperation)
            {
                if (TryGetAnonymousObjectCreationOperation(invocationOperation, out var anonymousObjectCreationOperation))
                    foreach (ISimpleAssignmentOperation initializer in anonymousObjectCreationOperation.Initializers)
                        AnalyzePropertyValue(context, initializer.Value);
            }

            private void AnalyzePropertyValue(OperationAnalysisContext context, IOperation propertyValue)
            {
                if (propertyValue.Type is null)
                    return;

                if (IsDecoratedWithSafeToLogAttribute(propertyValue.Type))
                {
                    if (GetPublicProperties(propertyValue.Type).Any(IsNotDecoratedWithNotSafeToLogAttribute))
                        return;
                }
                else
                {
                    if (GetPublicProperties(propertyValue.Type).Any(IsDecoratedWithSafeToLogAttribute))
                        return;
                }

                // "The '{0}' type does not have any properties marked as safe to log"
                var diagnostic = Diagnostic.Create(Rule, propertyValue.Syntax.GetLocation(), propertyValue.Type);
                context.ReportDiagnostic(diagnostic);
            }

            private bool TryGetAnonymousObjectCreationOperation(IInvocationOperation invocationOperation,
                out IAnonymousObjectCreationOperation anonymousObjectCreationOperation)
            {
                anonymousObjectCreationOperation = null;

                var extendedPropertiesArgument = GetExtendedPropertiesArgument();
                if (extendedPropertiesArgument == null)
                    return false;

                if (extendedPropertiesArgument.Value is IConversionOperation convertToObjectType
                    && SymbolEqualityComparer.Default.Equals(convertToObjectType.Type, _objectType))
                {
                    anonymousObjectCreationOperation = convertToObjectType.Operand as IAnonymousObjectCreationOperation;
                }
                else
                {
                    // TODO: Is it worth looking for the anonymousObjectCreationOperation anywhere else?
                }

                return anonymousObjectCreationOperation != null;

                IArgumentOperation GetExtendedPropertiesArgument()
                {
                    for (int i = 0; i < invocationOperation.TargetMethod.Parameters.Length; i++)
                        if (invocationOperation.TargetMethod.Parameters[i].Name == "extendedProperties")
                            return invocationOperation.Arguments[i];
                    return null;
                }
            }

            private bool IsDecoratedWithSafeToLogAttribute(ISymbol symbol) =>
                symbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _safeToLogAttributeType));

            private bool IsNotDecoratedWithNotSafeToLogAttribute(ISymbol symbol) =>
                !symbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _notSafeToLogAttributeType));

            private static IEnumerable<IPropertySymbol> GetPublicProperties(ITypeSymbol type) =>
                type.GetMembers().OfType<IPropertySymbol>().Where(p =>
                    p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);
        }
    }
}
