using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    internal class CatchParameterWalker : OperationWalker
    {
        private readonly IInvocationOperation _invocationOperation;

        public CatchParameterWalker(IInvocationOperation invocationOperation)
        {
            _invocationOperation = invocationOperation;
        }

        public bool IsExceptionCaught { get; private set; }

        public override void VisitCatchClause(ICatchClauseOperation catchClause)
        {
            if (catchClause.ExceptionDeclarationOrExpression is null)
                IsExceptionCaught = true;

            var argument = _invocationOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "exception");
            if (argument == null || argument.IsImplicit)
            {
                IsExceptionCaught = false;
            }
            else if (argument.Value is ILocalReferenceOperation localReference
            && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator)
            {
                var isException = localReference.Type.IsException();
                IsExceptionCaught = isException && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol);
            }
            else if (argument.Value is IConversionOperation conversion
                && conversion.Operand is ILocalReferenceOperation convertedLocalReference
                && !conversion.ConstantValue.HasValue
                && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation catchVariableDeclarator)
            {
                var doesCaughtExceptionMatchArgument = SymbolEqualityComparer.Default.Equals(convertedLocalReference.Local, catchVariableDeclarator.Symbol);
                IsExceptionCaught = conversion.Type.IsException() && doesCaughtExceptionMatchArgument;
            }

            base.VisitCatchClause(catchClause);
        }
    }
}
