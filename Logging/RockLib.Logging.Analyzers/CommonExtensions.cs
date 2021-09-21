﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System.Collections.Generic;
using System.Linq;
using RockLib.Analyzers.Json;
using System;

namespace RockLib.Logging.Analyzers
{
    public static class CommonExtensions
    {
        public static bool IsException(this ITypeSymbol typeSymbol, ITypeSymbol exceptionType, Compilation compilation)
        {
            return compilation.ClassifyCommonConversion(typeSymbol, exceptionType).IsImplicit;
        }

        public static bool TryGetMemberValue(this ObjectSyntax objectSyntax,
            string memberName, out JsonSyntaxNode value)
        {
            if (objectSyntax != null
                && objectSyntax.Members != null)
            {
                foreach (var member in objectSyntax.Members)
                {
                    if (member.Name != null && member.Name.IsValid && member.Value != null)
                    {
                        var name = member.Name.Value;
                        if (string.Equals(name, memberName, StringComparison.OrdinalIgnoreCase))
                        {
                            value = member.Value;
                            return true;
                        }
                    }
                }
            }

            value = null;
            return false;
        }

        public static bool TryGetMemberValue(this ObjectSyntax objectSyntax,
            string memberName, string alternateMemberName, out JsonSyntaxNode value)
        {
            if (objectSyntax != null
                && objectSyntax.Members != null)
            {
                foreach (var member in objectSyntax.Members)
                {
                    if (member.Name != null && member.Name.IsValid && member.Value != null)
                    {
                        var name = member.Name.Value;
                        if (string.Equals(name, memberName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(name, alternateMemberName, StringComparison.OrdinalIgnoreCase))
                        {
                            value = member.Value;
                            return true;
                        }
                    }
                }
            }

            value = null;
            return false;
        }

        public static IEnumerable<JsonSyntaxNode> GetItemValues(this ArraySyntax arraySyntax)
        {
            if (arraySyntax != null
                && arraySyntax.Items != null)
            {
                foreach (var item in arraySyntax.Items)
                    if (item.Value != null)
                        yield return item.Value;
            }
        }

        public static IObjectCreationOperation GetLogEntryCreationOperation(this IArgumentOperation logEntryArgument)
        {
            if (logEntryArgument.Value is IObjectCreationOperation objectCreation)
                return objectCreation;

            if (logEntryArgument.Value is ILocalReferenceOperation localReference)
                return Find<IObjectCreationOperation>.For(localReference);

            return null;
        }

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
                    anonymousObjectCreationOperation = Find<IAnonymousObjectCreationOperation>.For(localReferenceOperation);
                }
            }

            return anonymousObjectCreationOperation != null;
        }

        public static bool TryGetDictionaryExtendedPropertyValueOperations(this IOperation extendedPropertiesArgumentValue,
            out IEnumerable<IOperation> operations)
        {
            if (extendedPropertiesArgumentValue.Type.IsStringDictionaryType())
            {
                if (extendedPropertiesArgumentValue is ILocalReferenceOperation localReference)
                {
                    operations = GetDictionaryExtendedPropertyValueOperations(extendedPropertiesArgumentValue, localReference.Local);
                    return true;
                }
                else if (extendedPropertiesArgumentValue is IParameterReferenceOperation parameterReference)
                {
                    operations = GetDictionaryExtendedPropertyValueOperations(extendedPropertiesArgumentValue, parameterReference.Parameter);
                    return true;
                }
            }

            operations = Enumerable.Empty<IOperation>();
            return false;
        }

        public static IEnumerable<IPropertySymbol> GetPublicProperties(this ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Object)
                return Enumerable.Empty<IPropertySymbol>();

            var properties = GetProperties(type);

            while (true)
            {
                type = type.BaseType;
                if (type == null || type.SpecialType == SpecialType.System_Object)
                    break;
                properties = properties.Concat(GetProperties(type));
            }

            return properties;

            IEnumerable<IPropertySymbol> GetProperties(ITypeSymbol t) =>
                t.GetMembers().OfType<IPropertySymbol>().Where(p =>
                    p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);
        }

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

        public static bool IsStringDictionaryType(this ITypeSymbol typeSymbol) =>
            (typeSymbol is INamedTypeSymbol namedType && IsStringDictionary(namedType))
            || typeSymbol.Interfaces.Any(IsStringDictionary);

        private static bool IsStringDictionary(INamedTypeSymbol type) =>
            type.Name == "IDictionary"
                && type.IsGenericType
                && type.TypeArguments.Length == 2
                && type.TypeArguments[0].SpecialType == SpecialType.System_String;

        private static IEnumerable<IOperation> GetDictionaryExtendedPropertyValueOperations(
            IOperation extendedPropertiesArgumentValue, ISymbol extendedPropertiesSymbol)
        {
            var rootOperation = extendedPropertiesArgumentValue.GetRootOperation();

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
                if (semanticModel.GetOperation(node) is IInvocationOperation operation)
                {
                    if (!operation.TargetMethod.IsStatic
                        && (operation.TargetMethod.Name == "Add" || operation.TargetMethod.Name == "TryAdd")
                        && operation.TargetMethod.Parameters.Length == 2
                        && ((operation.Instance is ILocalReferenceOperation localReference
                            && SymbolEqualityComparer.Default.Equals(localReference.Local, extendedPropertiesSymbol))
                            || (operation.Instance is IParameterReferenceOperation parameterReference
                                && SymbolEqualityComparer.Default.Equals(parameterReference.Parameter, extendedPropertiesSymbol))))
                    {
                        return operation.Arguments[1].Value;
                    }

                    if (operation.TargetMethod.IsStatic
                        && operation.TargetMethod.Name == "TryAdd"
                        && operation.TargetMethod.Parameters.Length == 3
                        && ((operation.Arguments[0].Value is ILocalReferenceOperation localReference1
                            && SymbolEqualityComparer.Default.Equals(localReference1.Local, extendedPropertiesSymbol))
                            || (operation.Arguments[0].Value is IParameterReferenceOperation parameterReference1
                                && SymbolEqualityComparer.Default.Equals(parameterReference1.Parameter, extendedPropertiesSymbol))))
                    {
                        return operation.Arguments[2].Value;
                    }
                }
                return null;
            }

            IOperation TryGetDictionaryIndexerAssignmentValue(SyntaxNode node)
            {
                if (((ExpressionStatementSyntax)node).Expression is AssignmentExpressionSyntax assignmentExpression
                    && semanticModel.GetOperation(assignmentExpression) is ISimpleAssignmentOperation assignmentOperation
                    && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation
                    && ((propertyReferenceOperation.Instance is ILocalReferenceOperation localReference
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, extendedPropertiesSymbol))
                        || ((propertyReferenceOperation.Instance is IParameterReferenceOperation parameterReference
                            && SymbolEqualityComparer.Default.Equals(parameterReference.Parameter, extendedPropertiesSymbol))))
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
                        && objectCreation.Initializer != null)
                    {
                        foreach (var initializer in objectCreation.Initializer.Initializers)
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

        private class Find<TOperation> : OperationWalker
            where TOperation : IOperation
        {
            private readonly ILocalSymbol _localSymbol;
            private TOperation _foundOperation;

            private Find(ILocalSymbol localSymbol) => _localSymbol = localSymbol;

            public static TOperation For(ILocalReferenceOperation localReferenceOperation)
            {
                var operationWalker = new Find<TOperation>(localReferenceOperation.Local);
                operationWalker.Visit(localReferenceOperation.GetRootOperation());
                return operationWalker._foundOperation;
            }

            public override void VisitVariableDeclarator(IVariableDeclaratorOperation variableDeclaratorOperation)
            {
                if (variableDeclaratorOperation.Initializer is IVariableInitializerOperation variableInitializerOperation
                    && SymbolEqualityComparer.Default.Equals(_localSymbol, variableDeclaratorOperation.Symbol)
                    && variableInitializerOperation.Value is TOperation operation)
                {
                    _foundOperation = operation;
                }

                base.VisitVariableDeclarator(variableDeclaratorOperation);
            }
        }
    }
}
