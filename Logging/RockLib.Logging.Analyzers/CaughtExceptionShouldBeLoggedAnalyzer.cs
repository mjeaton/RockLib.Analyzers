using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CaughtExceptionShouldBeLoggedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Caught exception should be logged";
        private static readonly LocalizableString _messageFormat = "The caught exception should be passed into the logging method";
        private static readonly LocalizableString _description = "If a logging method is inside a catch block, the caught exception should be passed to it.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.CaughtExceptionShouldBeLogged,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.CaughtExceptionShouldBeLogged));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
            if (loggingExtensionsType == null)
                return;

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType == null)
                return;

            var loggerType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.ILogger");
            if (loggerType == null)
                return;

            var analyzer = new InvocationOperationAnalyzer(loggingExtensionsType, safeLoggingExtensionsType, loggerType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _loggingExtensionsType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;
            private readonly INamedTypeSymbol _loggerType;

            public InvocationOperationAnalyzer(INamedTypeSymbol loggingExtensionsType, INamedTypeSymbol safeLoggingExtensionsType,
                INamedTypeSymbol loggerType)
            {
                _loggingExtensionsType = loggingExtensionsType;
                _safeLoggingExtensionsType = safeLoggingExtensionsType;
                _loggerType = loggerType;
            }

            public void Analyze(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary
                    || !(GetCatchClause(invocationOperation) is ICatchClauseOperation catchClause))
                {
                    return;
                }

                if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggingExtensionsType)
                    || SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _safeLoggingExtensionsType))
                {
                    var visitor = new CatchParameterWalker(invocationOperation);
                    visitor.Visit(catchClause);
                    if (visitor.IsExceptionCaught)
                        return;

                    //if (ParameterCapturesCatchException(invocationOperation, catchClause))
                    //    return;
                }
                else if (methodSymbol.Name == "Log"
                    && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggerType))
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation();

                    if (logEntryCreation == null
                        || IsExceptionSet(logEntryCreation, logEntryArgument.Value, catchClause))
                    {
                        return;
                    }
                }
                else
                    return;

                var diagnostic = Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }

            private ICatchClauseOperation GetCatchClause(IInvocationOperation invocationOperation)
            {
                var parent = invocationOperation.Parent;
                while (parent != null)
                {
                    //TODO: Other catch operations?
                    if (parent is ICatchClauseOperation catchClause)
                        return catchClause;
                    parent = parent.Parent;
                }
                return null;
            }          

            private bool IsExceptionSet(IObjectCreationOperation logEntryCreation, IOperation logEntryArgumentValue,
                ICatchClauseOperation catchClause)
            {
                if (catchClause.ExceptionDeclarationOrExpression is null)
                    return false;

                if (logEntryCreation.Arguments.Length > 0)
                {
                    var exceptionArgument = logEntryCreation.Arguments.FirstOrDefault(a => a.Parameter.Name == "exception");
                    if (exceptionArgument != null
                        && !exceptionArgument.IsImplicit
                        && exceptionArgument.Value is ILocalReferenceOperation localReference
                        && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol))
                    {
                        return true;
                    }
                }

                if (logEntryCreation.Initializer != null)
                {
                    foreach (var initializer in logEntryCreation.Initializer.Initializers)
                    {
                        if (initializer is ISimpleAssignmentOperation assignment
                            && assignment.Target is IPropertyReferenceOperation property
                            && property.Property.Name == "Exception"
                            && assignment.Value is ILocalReferenceOperation localReference
                            && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator
                            && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol))
                        {
                            return true;
                        }
                    }
                }

                if (logEntryArgumentValue is ILocalReferenceOperation logEntryReference)
                {
                    var visitor = new SimpleAssignmentWalker(logEntryReference);
                    visitor.Visit(logEntryCreation.GetRootOperation());
                    return visitor.IsExceptionSet;
                }

                return false;
            }

            private class CatchParameterWalker : OperationWalker
            {
                private readonly IInvocationOperation _invocationOperation;

                public CatchParameterWalker(IInvocationOperation invocationOperation)
                {
                    _invocationOperation = invocationOperation;
                }

                public bool IsExceptionCaught { get; private set; }

                private bool IsException(ITypeSymbol symbol)
                {
                    if (symbol.Name.Equals("Exception"))
                    {
                        return true;
                    }

                    if (symbol.BaseType != null)
                    {
                        if (symbol.BaseType.Name.Equals("Exception"))
                        {
                            return true;
                        }
                        return IsException(symbol.BaseType);
                    }

                    return false;
                }

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
                        var isException = IsException(localReference.Type);
                        IsExceptionCaught = isException && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol);
                    }
                    else if (argument.Value is IConversionOperation conversion
                        && conversion.Operand is ILocalReferenceOperation convertedLocalReference
                        && !conversion.ConstantValue.HasValue
                        && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation catchVariableDeclarator)
                    {                        
                        var doesCaughtExceptionMatchArgument = SymbolEqualityComparer.Default.Equals(convertedLocalReference.Local, catchVariableDeclarator.Symbol);                        
                        IsExceptionCaught = IsException(conversion.Type) && doesCaughtExceptionMatchArgument;
                    }

                    base.VisitCatchClause(catchClause);
                }
            }

            private class SimpleAssignmentWalker : OperationWalker
            {
                private readonly ILocalReferenceOperation _logEntryReference;

                public SimpleAssignmentWalker(ILocalReferenceOperation logEntryReference)
                {
                    _logEntryReference = logEntryReference;
                }

                public bool IsExceptionSet { get; private set; }

                public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
                {
                    if (operation.Target is IPropertyReferenceOperation property
                        && property.Property.Name == "Exception"
                        && property.Instance is ILocalReferenceOperation localReference
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, _logEntryReference.Local))
                    {
                        IsExceptionSet = true;
                    }

                    base.VisitSimpleAssignment(operation);
                }
            }
        }
    }
}
