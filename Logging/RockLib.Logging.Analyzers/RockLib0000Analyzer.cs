using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
                var objectType = c.Compilation.GetTypeByMetadataName("System.Object");
                if (objectType == null)
                    return;

                var logEntryType = c.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
                if (logEntryType == null)
                    return;

                var safeLoggingExtensionsType = c.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
                if (safeLoggingExtensionsType == null)
                    return;

                var safeToLogAttributeType = c.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeToLogAttribute");
                if (safeToLogAttributeType == null)
                    return;

                var notSafeToLogAttributeType = c.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.NotSafeToLogAttribute");
                if (notSafeToLogAttributeType == null)
                    return;

                var analyzer = new InvocationOperationAnalyzer(objectType, logEntryType,
                    safeLoggingExtensionsType, safeToLogAttributeType, notSafeToLogAttributeType);

                c.RegisterOperationAction(
                    analyzer.Analyze,
                    OperationKind.Invocation);
            });
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

                var diagnostic = Diagnostic.Create(Rules.RockLib0000, propertyValue.Syntax.GetLocation());
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
