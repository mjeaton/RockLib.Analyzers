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
using System.Linq;
using System.Threading;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LoggerShouldBeSynchronousAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Logger should be synchronous";
        private static readonly LocalizableString _messageFormat = "Loggers used by RockLibLoggerProvider should be synchronous";
        private static readonly LocalizableString _description = "RockLibLoggerProvider has a dependency on a named instance of RockLib.Logging.ILogger, which should be have a synchronous processing mode. This is because context available to the logger is expected by the runtime to be consumed on the same thread.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.LoggerShouldBeSynchronous,
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
            var addLoggerExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.DependencyInjection.ServiceCollectionExtensions");
            if (addLoggerExtensionsType is null)
            {
                return;
            }

            var addRockLibLoggerProviderExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.RockLibLoggerProviderExtensions");
            if (addRockLibLoggerProviderExtensionsType is null)
            {
                return;
            }

            var analyzer = new OperationAnalyzer(addLoggerExtensionsType, addRockLibLoggerProviderExtensionsType);

            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
        }

        private class OperationAnalyzer
        {
            private readonly INamedTypeSymbol _addLoggerExtensionsType;
            private readonly INamedTypeSymbol _addRockLibLoggerProviderExtensionsType;

            public OperationAnalyzer(INamedTypeSymbol addLoggerExtensionsType, INamedTypeSymbol addRockLibLoggerProviderExtensionsType)
            {
                _addLoggerExtensionsType = addLoggerExtensionsType;
                _addRockLibLoggerProviderExtensionsType = addRockLibLoggerProviderExtensionsType;
            }

            public void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var addLoggerOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = addLoggerOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary
                    || !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _addLoggerExtensionsType))
                {
                    return;
                }

                if (addLoggerOperation.Arguments.FirstOrDefault(a => a.Parameter!.Name == "processingMode") is IArgumentOperation argument
                    && argument.Value is IFieldReferenceOperation field
                    && field.ConstantValue.HasValue
                    && Equals(field.ConstantValue.Value, 1))
                {
                    return;
                }

                if (!HasAddRockLibLoggerProviderInvocation(context.Compilation, addLoggerOperation, context.CancellationToken))
                {
                    return;
                }

                var invocationExpression = (InvocationExpressionSyntax)addLoggerOperation.Syntax;
                var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;
                var spanStart = memberAccessExpression.Name.SpanStart;
                var spanEnd = invocationExpression.ArgumentList.Span.End;
                var location = Location.Create(addLoggerOperation.Syntax.SyntaxTree, new TextSpan(spanStart, spanEnd - spanStart));

                var diagnostic = Diagnostic.Create(Rule, location);
                context.ReportDiagnostic(diagnostic);
            }

            private bool HasAddRockLibLoggerProviderInvocation(Compilation compilation,
                IInvocationOperation addLoggerOperation, CancellationToken cancellationToken)
            {
                // TODO: I think we should pass in the compilation to the constructor,
                // and let it start walking by calling Visit() in the ctor.
                var syntaxWalker = new SyntaxWalker(addLoggerOperation, _addRockLibLoggerProviderExtensionsType, cancellationToken);
                syntaxWalker.Visit(compilation);
                return syntaxWalker.HasAddRockLibLoggerProviderInvocation;
            }

            private sealed class SyntaxWalker 
                : CSharpSyntaxWalker
            {
                private readonly string _loggerName;
                private readonly INamedTypeSymbol _addRockLibLoggerProviderExtensionsType;
                private readonly CancellationToken _cancellationToken;
                private Compilation? _compilation;

                public SyntaxWalker(IInvocationOperation addLoggerOperation,
                    INamedTypeSymbol addRockLibLoggerProviderExtensionsType, CancellationToken cancellationToken)
                {
                    _loggerName = addLoggerOperation.Arguments.GetLoggerName();
                    _addRockLibLoggerProviderExtensionsType = addRockLibLoggerProviderExtensionsType;
                    _cancellationToken = cancellationToken;
                }

                public bool HasAddRockLibLoggerProviderInvocation { get; private set; }

                public void Visit(Compilation compilation)
                {
                    _compilation = compilation;
                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        Visit(syntaxTree.GetRoot(_cancellationToken));
                    }
                }

                public override void VisitInvocationExpression(InvocationExpressionSyntax node)
                {
                    if (node.Expression is MemberAccessExpressionSyntax memberAccess
                        && memberAccess.Name is IdentifierNameSyntax identifier
                        && identifier.Identifier.Text == "AddRockLibLoggerProvider"
                        && _compilation!.GetSemanticModel(node.SyntaxTree) is SemanticModel semanticModel
                        && semanticModel.GetOperation(node, _cancellationToken) is IInvocationOperation invocation
                        && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _addRockLibLoggerProviderExtensionsType)
                        && invocation.Arguments.GetLoggerName() == _loggerName)
                    {
                        HasAddRockLibLoggerProviderInvocation = true;
                    }

                    base.VisitInvocationExpression(node);
                }
            }
        }
    }
}
