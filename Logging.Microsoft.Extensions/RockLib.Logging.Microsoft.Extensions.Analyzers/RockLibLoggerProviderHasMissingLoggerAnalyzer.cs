using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RockLibLoggerProviderHasMissingLoggerAnalyzer 
        : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "RockLibLoggerProvider has missing logger";
        private static readonly LocalizableString _messageFormat = "A logger with the {0} has not been registered with the service collection";
        private static readonly LocalizableString _description = "RockLibLoggerProvider has a dependency on a named instance of RockLib.Logging.ILogger. If such a logger is not registered with the service collection, the RockLibLoggerProvider will fail to initialize.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.RockLibLoggerProviderHasMissingLogger,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.UseSanitizingLoggingMethod));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var rockLibLoggerProviderExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.RockLibLoggerProviderExtensions");
            if (rockLibLoggerProviderExtensionsType is null)
            {
                return;
            }

            var addLoggerExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.DependencyInjection.ServiceCollectionExtensions");
            if (addLoggerExtensionsType is null)
            {
                return;
            }

            var analyzer = new OperationAnalyzer(rockLibLoggerProviderExtensionsType, addLoggerExtensionsType);

            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
        }

        private sealed class OperationAnalyzer
        {
            private readonly INamedTypeSymbol _rockLibLoggerProviderExtensionsType;
            private readonly INamedTypeSymbol _addLoggerExtensionsType;

            public OperationAnalyzer(INamedTypeSymbol rockLibLoggerProviderExtensionsType, INamedTypeSymbol addLoggerExtensionsType)
            {
                _rockLibLoggerProviderExtensionsType = rockLibLoggerProviderExtensionsType;
                _addLoggerExtensionsType = addLoggerExtensionsType;
            }

            internal void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var addRockLibLoggerProviderOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = addRockLibLoggerProviderOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary
                    || !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _rockLibLoggerProviderExtensionsType)
                    || HasMatchingLogger(context.Compilation, addRockLibLoggerProviderOperation, context.CancellationToken))
                {
                    return;
                }

                var invocationExpression = (InvocationExpressionSyntax)addRockLibLoggerProviderOperation.Syntax;
                var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;
                var spanStart = memberAccessExpression.Name.SpanStart;
                var spanEnd = invocationExpression.ArgumentList.Span.End;
                var location = Location.Create(addRockLibLoggerProviderOperation.Syntax.SyntaxTree, new TextSpan(spanStart, spanEnd - spanStart));

                var loggerName = addRockLibLoggerProviderOperation.Arguments.GetLoggerName();
                var loggerNameMessage = loggerName.Length == 0 ? "default name" : $"name '{loggerName}'";

                var diagnostic = Diagnostic.Create(Rule, location, loggerNameMessage);
                context.ReportDiagnostic(diagnostic);
            }

            private bool HasMatchingLogger(Compilation compilation,
                IInvocationOperation addRockLibLoggerProviderOperation, CancellationToken cancellationToken)
            {
                // TODO: I think we should pass in the compilation to the constructor,
                // and let it start walking by calling Visit() in the ctor.
                var syntaxWalker = new SyntaxWalker(addRockLibLoggerProviderOperation, _addLoggerExtensionsType, cancellationToken);
                syntaxWalker.Visit(compilation);
                return syntaxWalker.HasMatchingLogger;
            }

            private sealed class SyntaxWalker 
                : CSharpSyntaxWalker
            {
                private readonly string _loggerName;
                private readonly INamedTypeSymbol _addLoggerExtensionsType;
                private readonly CancellationToken _cancellationToken;
                private Compilation? _compilation;

                public SyntaxWalker(IInvocationOperation addRockLibLoggerProviderOperation,
                    INamedTypeSymbol addLoggerExtensionsType, CancellationToken cancellationToken)
                {
                    _loggerName = addRockLibLoggerProviderOperation.Arguments.GetLoggerName();
                    _addLoggerExtensionsType = addLoggerExtensionsType;
                    _cancellationToken = cancellationToken;
                }

                public bool HasMatchingLogger { get; private set; }

                public void Visit(Compilation compilation)
                {
                    _compilation = compilation;
                    foreach (var syntaxTree in compilation.SyntaxTrees)
                        Visit(syntaxTree.GetRoot(_cancellationToken));
                }

                public override void VisitInvocationExpression(InvocationExpressionSyntax node)
                {
                    if (node.Expression is MemberAccessExpressionSyntax memberAccess
                        && memberAccess.Name is IdentifierNameSyntax identifier
                        && identifier.Identifier.Text == "AddLogger"
                        && _compilation!.GetSemanticModel(node.SyntaxTree) is SemanticModel semanticModel
                        && semanticModel.GetOperation(node, _cancellationToken) is IInvocationOperation invocation
                        && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _addLoggerExtensionsType)
                        && invocation.Arguments.GetLoggerName() == _loggerName)
                    {
                        HasMatchingLogger = true;
                    }

                    base.VisitInvocationExpression(node);
                }
            }
        }
    }
}
