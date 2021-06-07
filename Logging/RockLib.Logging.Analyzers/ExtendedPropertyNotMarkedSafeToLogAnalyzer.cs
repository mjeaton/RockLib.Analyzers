using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
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
            var enumType = context.Compilation.GetTypeByMetadataName("System.Enum");
            if (enumType == null)
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

            var analyzer = new InvocationOperationAnalyzer(enumType, logEntryType,
                safeLoggingExtensionsType, safeToLogAttributeType, notSafeToLogAttributeType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _enumType;
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;
            private readonly INamedTypeSymbol _safeToLogAttributeType;
            private readonly INamedTypeSymbol _notSafeToLogAttributeType;

            public InvocationOperationAnalyzer(INamedTypeSymbol enumType, INamedTypeSymbol logEntryType,
                INamedTypeSymbol safeLoggingExtensionsType, INamedTypeSymbol safeToLogAttributeType,
                INamedTypeSymbol notSafeToLogAttributeType)
            {
                _enumType = enumType;
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
                var extendedPropertiesArgument = GetExtendedPropertiesArgument();
                if (extendedPropertiesArgument == null
                    || !(extendedPropertiesArgument.Value is IConversionOperation convertToObjectType)
                    || convertToObjectType.Type.SpecialType != SpecialType.System_Object)
                {
                    return;
                }

                var extendedPropertiesArgumentValue = convertToObjectType.Operand;

                if (TryGetAnonymousObjectCreationOperation(extendedPropertiesArgumentValue, out var anonymousObjectCreationOperation))
                {
                    foreach (ISimpleAssignmentOperation initializer in anonymousObjectCreationOperation.Initializers)
                        AnalyzePropertyValue(initializer.Value, context.ReportDiagnostic);
                }
                else if (TryGetDictionaryExtendedPropertyValueOperations(extendedPropertiesArgumentValue, invocationOperation, out var dictionaryExtendedPropertyValues))
                {
                    foreach (var extendedPropertyValue in dictionaryExtendedPropertyValues)
                        AnalyzePropertyValue(extendedPropertyValue, context.ReportDiagnostic);
                }

                IArgumentOperation GetExtendedPropertiesArgument()
                {
                    for (int i = 0; i < invocationOperation.TargetMethod.Parameters.Length; i++)
                        if (invocationOperation.TargetMethod.Parameters[i].Name == "extendedProperties")
                            return invocationOperation.Arguments[i];
                    return null;
                }
            }

            private void AnalyzePropertyValue(IOperation propertyValue, Action<Diagnostic> reportDiagnostic)
            {
                if (propertyValue.Type is null || IsValueType(propertyValue.Type))
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
                reportDiagnostic(diagnostic);
            }

            private bool TryGetAnonymousObjectCreationOperation(IOperation extendedPropertiesArgumentValue,
                out IAnonymousObjectCreationOperation anonymousObjectCreationOperation)
            {
                anonymousObjectCreationOperation = null;

                if (extendedPropertiesArgumentValue.Type.IsAnonymousType)
                {
                    anonymousObjectCreationOperation = extendedPropertiesArgumentValue as IAnonymousObjectCreationOperation;

                    if (anonymousObjectCreationOperation is null
                        && extendedPropertiesArgumentValue is ILocalReferenceOperation localReferenceOperation)
                    {
                        var semanticModel = localReferenceOperation.SemanticModel;
                        var dataflow = semanticModel.AnalyzeDataFlow(localReferenceOperation.Syntax);

                        anonymousObjectCreationOperation =
                            dataflow.DataFlowsIn
                                .SelectMany(symbol => symbol.DeclaringSyntaxReferences.Select(GetAnonymousObjectCreationOperation))
                                .FirstOrDefault(operation => operation != null);

                        IAnonymousObjectCreationOperation GetAnonymousObjectCreationOperation(SyntaxReference syntaxReference)
                        {
                            var syntax = syntaxReference.GetSyntax();

                            if (semanticModel.GetOperation(syntax) is IVariableDeclaratorOperation variableDeclaratorOperation
                                && variableDeclaratorOperation.Initializer is IVariableInitializerOperation variableInitializerOperation)
                            {
                                return variableInitializerOperation.Value as IAnonymousObjectCreationOperation;
                            }

                            return null;
                        }
                    }
                }

                return anonymousObjectCreationOperation != null;
            }

            private bool TryGetDictionaryExtendedPropertyValueOperations(IOperation extendedPropertiesArgumentValue,
                IInvocationOperation invocationOperation, out IEnumerable<IOperation> operations)
            {
                if (IsStringDictionaryVariable(extendedPropertiesArgumentValue))
                {
                    operations = GetDictionaryExtendedPropertyValueOperations(invocationOperation, extendedPropertiesArgumentValue);
                    return true;
                }
                operations = Enumerable.Empty<IOperation>();
                return false;
            }

            private bool IsStringDictionaryVariable(IOperation extendedPropertiesArgumentValue) =>
                extendedPropertiesArgumentValue is ILocalReferenceOperation
                && extendedPropertiesArgumentValue.Type.Interfaces.Any(i =>
                       i.Name == "IDictionary"
                           && i.IsGenericType
                           && i.TypeArguments.Length == 2
                           && i.TypeArguments[0].SpecialType == SpecialType.System_String);

            private IEnumerable<IOperation> GetDictionaryExtendedPropertyValueOperations(IInvocationOperation invocationOperation,
                IOperation extendedPropertiesArgumentValue)
            {
                var extendedPropertiesSymbol = ((ILocalReferenceOperation)extendedPropertiesArgumentValue).Local;

                IOperation rootOperation = invocationOperation;
                while (rootOperation.Parent != null)
                    rootOperation = rootOperation.Parent;

                var semanticModel = rootOperation.SemanticModel;
                var descendants = rootOperation.Syntax.DescendantNodes(rootOperation.Syntax.GetLocation().SourceSpan);

                var addMethodValues = descendants
                    .Where(node => node.IsKind(SyntaxKind.InvocationExpression))
                    .Select(TryGetDictionaryAddMethodValue);

                var indexerAssignmentValues = descendants
                    .Where(node => node.IsKind(SyntaxKind.ExpressionStatement))
                    .Select(TryGetDictionaryIndexerAssignmentValue);

                var localDeclarationCollectionInitializerValues = descendants
                    .Where(node => node.IsKind(SyntaxKind.LocalDeclarationStatement))
                    .SelectMany(TryGetLocalDeclarationCollectionInitializerValues);

                var extendedPropertyValues = addMethodValues
                    .Concat(indexerAssignmentValues)
                    .Concat(localDeclarationCollectionInitializerValues)
                    .Where(value => value != null)
                    .Select(operation =>
                    {
                        if (operation is IConversionOperation conversionOperation)
                            return conversionOperation.Operand;
                        return operation;
                    });

                return extendedPropertyValues;

                IOperation TryGetDictionaryAddMethodValue(SyntaxNode node)
                {
                    if (semanticModel.GetOperation(node) is IInvocationOperation operation
                        && operation.TargetMethod.Name == "Add"
                        && operation.TargetMethod.Parameters.Length == 2
                        && operation.Instance is ILocalReferenceOperation localReference
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, extendedPropertiesSymbol))
                    {
                        return operation.Arguments[1].Value;
                    }
                    return null;
                }

                IOperation TryGetDictionaryIndexerAssignmentValue(SyntaxNode node)
                {
                    if (((ExpressionStatementSyntax)node).Expression is AssignmentExpressionSyntax assignmentExpression
                        && semanticModel.GetOperation(assignmentExpression) is ISimpleAssignmentOperation assignmentOperation
                        && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation
                        && propertyReferenceOperation.Instance is ILocalReferenceOperation localReference
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, extendedPropertiesSymbol)
                        && propertyReferenceOperation.Arguments.Length == 1
                        && propertyReferenceOperation.Arguments[0].Value.Type.SpecialType == SpecialType.System_String)
                    {
                        return assignmentOperation.Value;
                    }
                    return null;
                }

                IEnumerable<IOperation> TryGetLocalDeclarationCollectionInitializerValues(SyntaxNode node)
                {
                    var declaration = ((LocalDeclarationStatementSyntax)node).Declaration;
                    foreach (var variable in declaration.Variables)
                    {
                        var variableDeclarator = (IVariableDeclaratorOperation)semanticModel.GetOperation(variable);
                        if (SymbolEqualityComparer.Default.Equals(variableDeclarator.Symbol, extendedPropertiesSymbol)
                            && variableDeclarator.Initializer?.Value is IObjectCreationOperation objectCreation
                            && objectCreation.Initializer is IObjectOrCollectionInitializerOperation collectionInitializer)
                        {
                            foreach (var initializer in collectionInitializer.Initializers)
                            {
                                if (initializer is IInvocationOperation initializerInvocation
                                    && initializerInvocation.TargetMethod.Name == "Add"
                                    && initializerInvocation.Arguments.Length == 2)
                                {
                                    yield return initializerInvocation.Arguments[1].Value;
                                }
                                else if (initializer is ISimpleAssignmentOperation assignmentOperation
                                    && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation
                                    && propertyReferenceOperation.Arguments.Length == 1
                                    && propertyReferenceOperation.Arguments[0].Value.Type.SpecialType == SpecialType.System_String)
                                {
                                    yield return assignmentOperation.Value;
                                }
                            }
                        }
                    }
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

            private bool IsValueType(ITypeSymbol type)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_String:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_DateTime:
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                        return true;
                }

                if (SymbolEqualityComparer.Default.Equals(type.BaseType, _enumType))
                    return true;

                switch (type.ToString())
                {
                    case "System.TimeSpan":
                    case "System.DateTimeOffset":
                    case "System.Guid":
                    case "System.Uri":
                    case "System.Type":
                    case "System.Text.Encoding":
                        return true;
                }

                return false;
            }
        }
    }
}
