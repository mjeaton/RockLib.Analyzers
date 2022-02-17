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

namespace RockLib.Logging.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoLogLevelSpecifiedCodeFixProvider)), Shared]
    public sealed class NoLogLevelSpecifiedCodeFixProvider : CodeFixProvider
    {
        public const string SetLevelPropertyTo = "Set LogEntry.Level to ";
        public const string SetLevelParameterTo = "Set LogEntry.Level to ";

        private static readonly string[] _levels = { "Debug", "Info", "Warn", "Error", "Fatal", "Audit" };

        public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.NoLogLevelSpecified);

        public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async override sealed Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;
            var semanticModel = (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false))!;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                var logEntryArgument = (IArgumentOperation)semanticModel.GetOperation(node)!;
                var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation();

                if (logEntryCreation?.Arguments.Length > 0)
                {
                    foreach (var level in _levels)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: SetLevelParameterTo + level,
                                createChangedDocument: cancellationToken =>
                                    SetLevelParameter(context.Document, logEntryCreation, level, cancellationToken),
                                equivalenceKey: nameof(SetLevelParameterTo) + level),
                            diagnostic);
                    }
                }
                else
                {
                    foreach (var level in _levels)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: SetLevelPropertyTo + level,
                                createChangedDocument: cancellationToken =>
                                    SetLevelProperty(context.Document, logEntryCreation!, level, cancellationToken),
                                equivalenceKey: nameof(SetLevelPropertyTo) + level),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> SetLevelProperty(
            Document document,
            IObjectCreationOperation logEntryCreationOperation,
            string logLevel,
            CancellationToken cancellationToken)
        {
            var logEntryCreationExpression = (BaseObjectCreationExpressionSyntax)logEntryCreationOperation.Syntax;
            BaseObjectCreationExpressionSyntax replacementLogEntryCreationExpression;

            var levelAssignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("Level"),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("LogLevel"),
                    SyntaxFactory.IdentifierName(logLevel)));

            if (logEntryCreationOperation.Initializer is not null)
            {
                replacementLogEntryCreationExpression = logEntryCreationExpression.WithInitializer(
                    logEntryCreationExpression.Initializer!.WithExpressions(
                        SyntaxFactory.SeparatedList(
                            logEntryCreationExpression.Initializer.Expressions
                                .Concat(new[] { levelAssignment }))));
            }
            else
            {
                replacementLogEntryCreationExpression = logEntryCreationExpression.WithInitializer(
                    SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(new[] { levelAssignment })));
            }

            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

            root = root.ReplaceNode(logEntryCreationExpression, replacementLogEntryCreationExpression);

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> SetLevelParameter(
            Document document,
            IObjectCreationOperation logEntryCreationOperation,
            string logLevel,
            CancellationToken cancellationToken)
        {
            var logEntryCreationExpression = (BaseObjectCreationExpressionSyntax)logEntryCreationOperation.Syntax!;

            var levelArgument = SyntaxFactory.Argument(
                SyntaxFactory.NameColon("level"),
                default,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("LogLevel"),
                    SyntaxFactory.IdentifierName(logLevel)));

            var replacementLogEntryCreationExpression =
                logEntryCreationExpression.WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            logEntryCreationExpression.ArgumentList!.Arguments
                                .Concat(new[] { levelArgument }))));

            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

            root = root.ReplaceNode(logEntryCreationExpression, replacementLogEntryCreationExpression);

            return document.WithSyntaxRoot(root);
        }
    }
}
