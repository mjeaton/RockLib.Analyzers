using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using RockLib.Analyzers.Common;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseSynchronousLoggerWithRockLibLoggerProviderAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Use synchronous logger when using RockLibLoggerProvider";
        private static readonly LocalizableString _messageFormat = "Use synchronous logger when using RockLibLoggerProvider";
        private static readonly LocalizableString _description = "When using RockLibLoggerProvider, the RockLib logger should be have a synchronous processing mode. This is because context available to the logger is expected by the runtime to be consumed on the same thread.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.UseSynchronousLoggerWithRockLibLoggerProvider,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.UseSanitizingLoggingMethod));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var addLoggerExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.DependencyInjection.ServiceCollectionExtensions");
            if (addLoggerExtensionsType == null)
                return;

            var addRockLibLoggerProviderExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.RockLibLoggerProviderExtensions");
            if (addRockLibLoggerProviderExtensionsType == null)
                return;

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

                if (addLoggerOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "processingMode") is IArgumentOperation argument
                    && argument.Value is IFieldReferenceOperation field
                    && field.ConstantValue.HasValue
                    && Equals(field.ConstantValue.Value, 1))
                {
                    return;
                }

                if (!HasAddRockLibLoggerProviderInvocation(context.Compilation, addLoggerOperation, context.CancellationToken))
                    return;

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
                var syntaxWalker = new SyntaxWalker(addLoggerOperation, _addRockLibLoggerProviderExtensionsType, cancellationToken);
                syntaxWalker.Visit(compilation);
                return syntaxWalker.HasAddRockLibLoggerProviderInvocation;
            }

            private class SyntaxWalker : CSharpSyntaxWalker
            {
                private readonly string _loggerName;
                private readonly INamedTypeSymbol _addRockLibLoggerProviderExtensionsType;
                private readonly CancellationToken _cancellationToken;
                private readonly SemanticModel _semanticModel;

                public SyntaxWalker(IInvocationOperation addLoggerOperation,
                    INamedTypeSymbol addRockLibLoggerProviderExtensionsType, CancellationToken cancellationToken)
                {
                    _loggerName = GetLoggerName(addLoggerOperation.Arguments);
                    _addRockLibLoggerProviderExtensionsType = addRockLibLoggerProviderExtensionsType;
                    _cancellationToken = cancellationToken;
                    _semanticModel = addLoggerOperation.GetRootOperation().SemanticModel;
                }

                public bool HasAddRockLibLoggerProviderInvocation { get; private set; }

                public void Visit(Compilation compilation)
                {
                    foreach (var syntaxTree in compilation.SyntaxTrees)
                        Visit(syntaxTree.GetRoot(_cancellationToken));
                }

                public override void VisitInvocationExpression(InvocationExpressionSyntax node)
                {
                    if (node.Expression is MemberAccessExpressionSyntax memberAccess
                        && memberAccess.Name is IdentifierNameSyntax identifier
                        && identifier.Identifier.Text == "AddRockLibLoggerProvider"
                        && _semanticModel.GetOperation(node, _cancellationToken) is IInvocationOperation invocation
                        && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _addRockLibLoggerProviderExtensionsType)
                        && GetLoggerName(invocation.Arguments) == _loggerName)
                    {
                        HasAddRockLibLoggerProviderInvocation = true;
                    }

                    base.VisitInvocationExpression(node);
                }

                private static string GetLoggerName(ImmutableArray<IArgumentOperation> arguments)
                {
                    if (arguments.FirstOrDefault(IsLoggerNameArgument) is IArgumentOperation argument
                        && argument.Value is ILiteralOperation literal
                        && literal.ConstantValue.HasValue)
                    {
                        return (string)literal.ConstantValue.Value;
                    }

                    return "";

                    bool IsLoggerNameArgument(IArgumentOperation arg) =>
                        arg.Parameter.Name == "loggerName" || arg.Parameter.Name == "rockLibLoggerName";
                }
            }
        }
    }
}
