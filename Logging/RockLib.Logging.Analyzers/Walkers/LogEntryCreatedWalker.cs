using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    internal sealed class LogEntryCreatedWalker : OperationWalker
    {
        private readonly IOperation _logEntryArgumentValue;
        private readonly IObjectCreationOperation _createOperation;
        private readonly ITypeSymbol _exceptionType;
        private readonly Compilation _compilation;

        public LogEntryCreatedWalker(IOperation logEntryArgumentValue,
            IObjectCreationOperation createOperation,
            ITypeSymbol exceptionType,
            Compilation compilation)
        {
            _logEntryArgumentValue = logEntryArgumentValue;
            _createOperation = createOperation;
            _exceptionType = exceptionType;
            _compilation = compilation;
            Visit(createOperation.GetRootOperation());
        }

        public bool IsExceptionSet { get; private set; }

        public override void VisitCatchClause(ICatchClauseOperation catchClause)
        {
            if (IsExceptionSet)
            {
                return;
            }

            if (_createOperation.Arguments.Length > 0)
            {
                var exceptionArgument = _createOperation.Arguments.FirstOrDefault(a => a.Parameter!.Name == "exception");
                if (exceptionArgument is not null
                    && !exceptionArgument.IsImplicit
                    && exceptionArgument.Value is ILocalReferenceOperation localReference
                    && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator
                    && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol))
                {
                    IsExceptionSet = true;
                    return;
                }
                else if (exceptionArgument is not null
                    && exceptionArgument.Value is IConversionOperation conversion
                    && conversion.Operand is ILocalReferenceOperation convertedLocalReference
                    && !conversion.ConstantValue.HasValue
                    && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation catchVariableDeclarator)
                {
                    var doesCaughtExceptionMatchArgument = SymbolEqualityComparer.Default.Equals(convertedLocalReference.Local, catchVariableDeclarator.Symbol);
                    IsExceptionSet = conversion.Type!.IsException(_exceptionType, _compilation)
                       && doesCaughtExceptionMatchArgument;
                    return;
                }
            }

            if (_createOperation.Initializer is not null)
            {
                foreach (var initializer in _createOperation.Initializer.Initializers)
                {
                    if (initializer is ISimpleAssignmentOperation assignment
                        && assignment.Target is IPropertyReferenceOperation property
                        && property.Property.Name == "Exception"
                        && assignment.Value is ILocalReferenceOperation localReference
                        && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol))
                    {
                        IsExceptionSet = true;
                        return;
                    }

                    if (initializer is ISimpleAssignmentOperation conversionAssignment
                       && conversionAssignment.Target is IPropertyReferenceOperation propertyRef
                       && propertyRef.Property.Name == "Exception"
                       && conversionAssignment.Value is IConversionOperation conversionOperation
                       && conversionOperation.Operand is ILocalReferenceOperation localRef
                       && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation caughtVariable
                       && localRef.Local.Type.IsException(_exceptionType, _compilation)
                       && SymbolEqualityComparer.Default.Equals(localRef.Local, caughtVariable.Symbol))
                    {
                        IsExceptionSet = true;
                        return;
                    }
                }
            }

            if (_logEntryArgumentValue is ILocalReferenceOperation logEntryReference)
            {
                var visitor = new SimpleAssignmentWalker(logEntryReference, _createOperation.GetRootOperation());
                IsExceptionSet = visitor.IsExceptionSet;
                return;
            }

            base.VisitCatchClause(catchClause);
        }
    }
}
