using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
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
                    if (ParameterCapturesCatchException(invocationOperation, catchClause))
                        return;
                }
                else if (methodSymbol.Name == "Log"
                    && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggerType))
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = GetLogEntryCreationOperation(logEntryArgument);

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

            private bool ParameterCapturesCatchException(IInvocationOperation invocationOperation,
                ICatchClauseOperation catchClause)
            {
                if (catchClause.ExceptionDeclarationOrExpression is null)
                    return false;

                var argument = invocationOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "exception");
                if (argument == null || argument.IsImplicit)
                    return false;

                if (argument.Value is ILocalReferenceOperation localReference
                    && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator)
                {
                    return SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol);
                }

                return false;
            }

            private IObjectCreationOperation GetLogEntryCreationOperation(IArgumentOperation logEntryArgument)
            {
                if (logEntryArgument.Value is IObjectCreationOperation objectCreation)
                    return objectCreation;

                if (logEntryArgument.Value is ILocalReferenceOperation localReference)
                {
                    var semanticModel = localReference.SemanticModel;
                    var dataflow = semanticModel.AnalyzeDataFlow(localReference.Syntax);

                    return dataflow.DataFlowsIn
                        .SelectMany(symbol => symbol.DeclaringSyntaxReferences.Select(GetObjectCreationOperation))
                        .FirstOrDefault(operation => operation != null);

                    IObjectCreationOperation GetObjectCreationOperation(SyntaxReference syntaxReference)
                    {
                        var syntax = syntaxReference.GetSyntax();

                        if (semanticModel.GetOperation(syntax) is IVariableDeclaratorOperation variableDeclaratorOperation
                            && variableDeclaratorOperation.Initializer is IVariableInitializerOperation variableInitializerOperation)
                        {
                            return variableInitializerOperation.Value as IObjectCreationOperation;
                        }

                        return null;
                    }
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
                    if (exceptionArgument != null && !exceptionArgument.IsImplicit)
                    {
                        if (exceptionArgument.Value is ILocalReferenceOperation localReference
                            && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator
                            && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol))
                        {
                            return true;
                        }
                    }
                }

                if (logEntryCreation.Initializer != null)
                {
                    foreach (var initializer in logEntryCreation.Initializer.Initializers)
                    {
                        if (initializer is ISimpleAssignmentOperation assignment
                            && assignment.Target is IPropertyReferenceOperation property
                            && property.Property.Name == "Exception")
                        {
                            if (assignment.Value is ILocalReferenceOperation localReference
                                && catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator
                                && SymbolEqualityComparer.Default.Equals(localReference.Local, variableDeclarator.Symbol))
                            {
                                return true;
                            }
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
