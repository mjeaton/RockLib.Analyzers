using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
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

        private void AnalyzeInvocationOperation(OperationAnalysisContext context,
            INamedTypeSymbol logEntryType, INamedTypeSymbol safeLoggingExtensionsType)
        {
            var invocationOperation = (IInvocationOperation)context.Operation;
            var methodSymbol = invocationOperation.TargetMethod;

            if (methodSymbol.MethodKind != MethodKind.Ordinary)
                return;

            if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, safeLoggingExtensionsType))
                AnalyzeSanitizingLoggingExtensionMethodCall(context, invocationOperation);
            else if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, logEntryType))
                if (methodSymbol.Name == "SetSanitizedExtendedProperty")
                    AnalyzeSetSanitizedExtendedPropertyMethodCall(context, invocationOperation);
                else if (methodSymbol.Name == "SetSanitizedExtendedProperties")
                    AnalyzeSetSanitizedExtendedPropertiesMethodCall(context, invocationOperation);
        }

        private void AnalyzeSetSanitizedExtendedPropertyMethodCall(OperationAnalysisContext context,
            IInvocationOperation invocationOperation)
        {
            var valueArgument = invocationOperation.Arguments[1];
            if (valueArgument.Value is IConversionOperation convertToObjectType
                && convertToObjectType.Type.Name == nameof(Object)
                && convertToObjectType.Type.ContainingNamespace?.Name == "System")
            {
                AnalyzePropertyType(context, convertToObjectType.Operand.Type, convertToObjectType.Operand);
            }
        }

        private void AnalyzeSetSanitizedExtendedPropertiesMethodCall(OperationAnalysisContext context,
            IInvocationOperation invocationOperation)
        {
            AnalyzeExtendedPropertiesArgument(context, invocationOperation);
        }

        private void AnalyzeSanitizingLoggingExtensionMethodCall(OperationAnalysisContext context,
            IInvocationOperation invocationOperation)
        {
            AnalyzeExtendedPropertiesArgument(context, invocationOperation);
        }

        private void AnalyzeExtendedPropertiesArgument(OperationAnalysisContext context, IInvocationOperation invocationOperation)
        {
            var extendedPropertiesArgument = GetExtendedPropertiesArgument(invocationOperation);

            if (extendedPropertiesArgument?.Value is IConversionOperation convertToObjectType
                && convertToObjectType.Type.Name == nameof(Object)
                && convertToObjectType.Type.ContainingNamespace?.Name == "System"
                && convertToObjectType.Operand is IAnonymousObjectCreationOperation createAnonymousObject)
            {
                foreach (ISimpleAssignmentOperation initializer in createAnonymousObject.Initializers)
                {
                    string propertyTypeName = $"{initializer.Type.ContainingNamespace?.Name}.{initializer.Type.Name}";
                    var propertyType = context.Compilation.GetTypeByMetadataName(propertyTypeName);
                    if (propertyType != null)
                        AnalyzePropertyType(context, propertyType, initializer.Value);
                }
            }
        }

        private void AnalyzePropertyType(OperationAnalysisContext context, ITypeSymbol propertyType, IOperation propertyValue)
        {
            if (propertyType is INamedTypeSymbol namedTypeSymbol)
                AnalyzePropertyType(context, namedTypeSymbol, propertyValue);
        }

        private void AnalyzePropertyType(OperationAnalysisContext context, INamedTypeSymbol namedType, IOperation propertyValue)
        {
            if (IsDecoratedWithSafeToLogAttribute(namedType))
            {
                if (GetPublicProperties(namedType).Any(IsNotDecoratedWithNotSafeToLogAttribute))
                    return;

                var diagnostic = Diagnostic.Create(Rules.RockLib0000, propertyValue.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                if (GetPublicProperties(namedType).Any(IsDecoratedWithSafeToLogAttribute))
                    return;

                var diagnostic = Diagnostic.Create(Rules.RockLib0000, propertyValue.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsDecoratedWithSafeToLogAttribute(ISymbol symbol) =>
            symbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == "SafeToLogAttribute"
                && attr.AttributeClass?.ContainingNamespace.Name == "SafeLogging"
                && attr.AttributeClass?.ContainingNamespace.ContainingNamespace.Name == "Logging"
                && attr.AttributeClass?.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name == "RockLib");

        private static bool IsNotDecoratedWithNotSafeToLogAttribute(ISymbol symbol) =>
            !symbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == "NotSafeToLogAttribute"
                && attr.AttributeClass?.ContainingNamespace.Name == "SafeLogging"
                && attr.AttributeClass?.ContainingNamespace.ContainingNamespace.Name == "Logging"
                && attr.AttributeClass?.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name == "RockLib");

        private static IEnumerable<IPropertySymbol> GetPublicProperties(INamedTypeSymbol namedType) =>
            namedType.GetMembers().OfType<IPropertySymbol>().Where(p =>
                !p.IsIndexer && p.DeclaredAccessibility == Accessibility.Public);

        private static IArgumentOperation GetExtendedPropertiesArgument(IInvocationOperation invocationOperation)
        {
            for (int i = 0; i < invocationOperation.TargetMethod.Parameters.Length; i++)
                if (invocationOperation.TargetMethod.Parameters[i].Name == "extendedProperties")
                    return invocationOperation.Arguments[i];
            return null;
        }
    }
}
