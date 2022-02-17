using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Immutable;
using System.Globalization;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CaughtExceptionShouldBeLoggedAnalyzer : DiagnosticAnalyzer
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
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.CaughtExceptionShouldBeLogged));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null) { throw new ArgumentNullException(nameof(context)); }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
            if (loggingExtensionsType is null) { return; }

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType is null) { return; }

            var loggerType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.ILogger");
            if (loggerType is null) { return; }

            var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionType is null) { return; }

            var analyzer = new InvocationOperationAnalyzer(loggingExtensionsType, exceptionType, safeLoggingExtensionsType, loggerType);
            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private sealed class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _loggingExtensionsType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;
            private readonly INamedTypeSymbol _loggerType;
            private readonly INamedTypeSymbol _exceptionType;

            public InvocationOperationAnalyzer(INamedTypeSymbol loggingExtensionsType,
                INamedTypeSymbol exceptionType,
                INamedTypeSymbol safeLoggingExtensionsType,
                INamedTypeSymbol loggerType)
            {
                _loggingExtensionsType = loggingExtensionsType;
                _exceptionType = exceptionType;
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
                    var visitor = new CatchParameterWalker(invocationOperation, _exceptionType, context.Compilation, catchClause);
                    if (visitor.IsExceptionCaught)
                    {
                        return;
                    }
                }
                else if (methodSymbol.Name == "Log"
                    && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggerType))
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation();

                    if (logEntryCreation is null
                        || IsExceptionSet(logEntryCreation, logEntryArgument.Value, catchClause, context.Compilation))
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }

            private static ICatchClauseOperation? GetCatchClause(IInvocationOperation invocationOperation)
            {
                var parent = invocationOperation.Parent;
                while (parent is not null)
                {
                    //TODO: Other catch operations?
                    if (parent is ICatchClauseOperation catchClause)
                    {
                        return catchClause;
                    }
                    parent = parent.Parent;
                }
                return null;
            }

            private bool IsExceptionSet(IObjectCreationOperation logEntryCreation, IOperation logEntryArgumentValue,
                ICatchClauseOperation catchClause, Compilation compilation)
            {
                if (catchClause.ExceptionDeclarationOrExpression is not null)
                {
                    var logWalker = new LogEntryCreatedWalker(logEntryArgumentValue, logEntryCreation, _exceptionType, compilation);
                    return logWalker.IsExceptionSet;
                }

                return false;
            }
        }
    }
}
