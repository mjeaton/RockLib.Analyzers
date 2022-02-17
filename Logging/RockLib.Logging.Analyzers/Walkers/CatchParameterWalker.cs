using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    internal sealed class CatchParameterWalker : OperationWalker
    {
        private readonly IInvocationOperation _invocationOperation;
        private readonly ITypeSymbol _exceptionType;
        private readonly Compilation _compilation;

        internal CatchParameterWalker(IInvocationOperation invocationOperation, ITypeSymbol exceptionType, Compilation compilation,
            ICatchClauseOperation catchClause)
        {
            _invocationOperation = invocationOperation;
            _exceptionType = exceptionType;
            _compilation = compilation;

            VisitCatchClause(catchClause);
        }

        internal bool IsExceptionCaught { get; private set; }

        public override void VisitCatchClause(ICatchClauseOperation catchClause)
        {
            if (catchClause.ExceptionDeclarationOrExpression is null)
            {
                IsExceptionCaught = true;
            }

            var argument = _invocationOperation.Arguments.FirstOrDefault(a => a.Parameter!.Name == "exception");
            if (argument is null || argument.IsImplicit)
            {
                IsExceptionCaught = false;
            }
            else if (argument.Value is ILocalReferenceOperation localReference
                && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator)
            {
                var isException = localReference.Type!.IsException(_exceptionType, _compilation);
                IsExceptionCaught = isException && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol);
            }
            else if (argument.Value is IConversionOperation conversion
                && conversion.Operand is ILocalReferenceOperation convertedLocalReference
                && !conversion.ConstantValue.HasValue
                && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation catchVariableDeclarator)
            {
                var isEx = _compilation.ClassifyCommonConversion(convertedLocalReference.Type!, _exceptionType).IsImplicit;
                var doesCaughtExceptionMatchArgument = SymbolEqualityComparer.Default.Equals(convertedLocalReference.Local, catchVariableDeclarator.Symbol);
                IsExceptionCaught = convertedLocalReference.Type!.IsException(_exceptionType, _compilation) && doesCaughtExceptionMatchArgument;
            }

            base.VisitCatchClause(catchClause);
        }
    }
}
