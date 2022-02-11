using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoggerShouldBeSynchronousCodeFixProvider)), Shared]
    public sealed class LoggerShouldBeSynchronousCodeFixProvider 
        : CodeFixProvider
    {
        public const string AddSynchronousProcessingModeArgumentTitle = "Add 'processingMode' argument with a value of Logger.ProcessingMode.Synchronous";
        public const string ChangeProcessingModeArgumentToSynchronousTitle = "Change 'processingMode' argument to Logger.ProcessingMode.Synchronous";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.LoggerShouldBeSynchronous);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            if (semanticModel is null)
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root!.FindNode(diagnostic.Location.SourceSpan);

                if (node is InvocationExpressionSyntax invocationExpression
                    && semanticModel.GetOperation(node, context.CancellationToken) is IInvocationOperation invocationOperation)
                {
                    var processingModeArgumentOperation = invocationOperation.Arguments.FirstOrDefault(a => a.Parameter!.Name == "processingMode");

                    if (processingModeArgumentOperation is null)
                    {
                        continue;
                    }

                    if (processingModeArgumentOperation.IsImplicit)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: AddSynchronousProcessingModeArgumentTitle,
                                createChangedDocument: cancellationToken =>
                                    AddSynchronousProcessingModeArgumentAsync(
                                        context.Document, invocationExpression, cancellationToken),
                                equivalenceKey: nameof(AddSynchronousProcessingModeArgumentTitle)),
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: ChangeProcessingModeArgumentToSynchronousTitle,
                                createChangedDocument: cancellationToken =>
                                    ChangeProcessingModeArgumentToSynchronousAsync(
                                        context.Document, invocationOperation, cancellationToken),
                                equivalenceKey: nameof(ChangeProcessingModeArgumentToSynchronousTitle)),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> AddSynchronousProcessingModeArgumentAsync(
            Document document,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken)
        {
            var arguments = invocationExpression.ArgumentList.Arguments;

            var replacementArgumentList =
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        arguments.Concat(new[] {
                            SyntaxFactory.Argument(
                                SyntaxFactory.NameColon("processingMode"),
                                default,
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("Logger"),
                                        SyntaxFactory.IdentifierName("ProcessingMode")),
                                    SyntaxFactory.IdentifierName("Synchronous"))
                                ) }))).WithTriviaFrom(invocationExpression.ArgumentList);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            root = root!.ReplaceNode(invocationExpression.ArgumentList, replacementArgumentList);

            if (root is CompilationUnitSyntax compilationUnit
                && !compilationUnit.Usings.Any(u => u.Name.ToFullString() == "RockLib.Logging"))
            {
                var name = SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("RockLib"),
                    SyntaxFactory.IdentifierName("Logging"));
                root = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(name));
            }

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> ChangeProcessingModeArgumentToSynchronousAsync(
            Document document,
            IInvocationOperation invocationOperation,
            CancellationToken cancellationToken)
        {
            var processingModeArgument = (ArgumentSyntax)invocationOperation.Arguments.Single(a => a.Parameter!.Name == "processingMode").Syntax;

            var replacementArgument = processingModeArgument.WithExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Logger"),
                        SyntaxFactory.IdentifierName("ProcessingMode")),
                    SyntaxFactory.IdentifierName("Synchronous")));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            root = root!.ReplaceNode(processingModeArgument, replacementArgument);

            return document.WithSyntaxRoot(root);
        }
    }
}
