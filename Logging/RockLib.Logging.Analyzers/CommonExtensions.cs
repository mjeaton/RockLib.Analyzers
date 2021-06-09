using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    internal static class CommonExtensions
    {
        public static bool TryGetAnonymousObjectCreationOperation(this IOperation extendedPropertiesArgumentValue,
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

        public static bool TryGetDictionaryExtendedPropertyValueOperations(this IOperation extendedPropertiesArgumentValue,
            out IEnumerable<IOperation> operations)
        {
            if (IsStringDictionaryVariable(extendedPropertiesArgumentValue))
            {
                operations = GetDictionaryExtendedPropertyValueOperations(extendedPropertiesArgumentValue);
                return true;
            }
            operations = Enumerable.Empty<IOperation>();
            return false;
        }

        public static IEnumerable<IPropertySymbol> GetPublicProperties(this ITypeSymbol type) =>
            type.GetMembers().OfType<IPropertySymbol>().Where(p =>
                p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);

        public static bool IsValueType(this ITypeSymbol type)
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

            if (type.BaseType != null && type.BaseType.ToString() == "System.Enum")
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

        private static bool IsStringDictionaryVariable(IOperation extendedPropertiesArgumentValue) =>
            extendedPropertiesArgumentValue is ILocalReferenceOperation
            && extendedPropertiesArgumentValue.Type.Interfaces.Any(i =>
                   i.Name == "IDictionary"
                       && i.IsGenericType
                       && i.TypeArguments.Length == 2
                       && i.TypeArguments[0].SpecialType == SpecialType.System_String);

        private static IEnumerable<IOperation> GetDictionaryExtendedPropertyValueOperations(IOperation extendedPropertiesArgumentValue)
        {
            var extendedPropertiesSymbol = ((ILocalReferenceOperation)extendedPropertiesArgumentValue).Local;

            IOperation rootOperation = extendedPropertiesArgumentValue;
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
    }
}
