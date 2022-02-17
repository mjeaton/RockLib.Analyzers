using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RockLib.Logging.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CaughtExceptionShouldBeLoggedCodeFixProvider)), Shared]
    public sealed class CaughtExceptionShouldBeLoggedCodeFixProvider : CodeFixProvider
    {
        public const string PassExceptionToLogEntryConstructorTitle = "Pass exception to LogEntry constructor";
        public const string SetLogEntryExceptionPropertyTitle = "Set LogEntry.Exception property";
        public const string PassExceptionToLoggingExtensionMethodTitle = "Pass exception to logging extension method";
        
        public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.CaughtExceptionShouldBeLogged);

        public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async override sealed Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;
            var semanticModel = (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false))!;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                var invocationOperation = (IInvocationOperation)semanticModel.GetOperation(node)!;

                if (invocationOperation.TargetMethod.Name == "Log")
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation()!;

                    if (logEntryCreation.Arguments.Length > 0)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: PassExceptionToLogEntryConstructorTitle,
                                createChangedDocument: cancellationToken =>
                                    PassExceptionToLogEntryConstructor(invocationOperation, context.Document, logEntryCreation, cancellationToken),
                                equivalenceKey: nameof(PassExceptionToLogEntryConstructorTitle)),
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: SetLogEntryExceptionPropertyTitle,
                                createChangedDocument: cancellationToken =>
                                    SetLogEntryExceptionProperty(invocationOperation, context.Document, logEntryCreation, cancellationToken),
                                equivalenceKey: nameof(SetLogEntryExceptionPropertyTitle)),
                            diagnostic);
                    }
                }
                else
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: PassExceptionToLoggingExtensionMethodTitle,
                            createChangedDocument: cancellationToken =>
                                PassExceptionToLoggingExtensionMethod(invocationOperation, context.Document, cancellationToken),
                            equivalenceKey: nameof(PassExceptionToLoggingExtensionMethodTitle)),
                        diagnostic);
                }
            }
        }

        private static async Task<Document> PassExceptionToLogEntryConstructor(
            IInvocationOperation invocationOperation,
            Document document,
            IObjectCreationOperation logEntryCreationOperation,
            CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

            var exceptionParameter = GetExceptionParameter(invocationOperation, out var needToFixCatchClause);

            var logEntryCreationExpression = (BaseObjectCreationExpressionSyntax)logEntryCreationOperation.Syntax;
            var arguments = AddExceptionArgument(logEntryCreationExpression.ArgumentList!.Arguments, exceptionParameter, logEntryCreationOperation.Arguments);

            var replacementLogEntryCreationExpression = logEntryCreationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            if (needToFixCatchClause)
            {
                return await FixCatchClause(invocationOperation, document, root, logEntryCreationExpression, replacementLogEntryCreationExpression, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                root = root.ReplaceNode(logEntryCreationExpression, replacementLogEntryCreationExpression);
                return document.WithSyntaxRoot(root);
            }
        }

        private static async Task<Document> SetLogEntryExceptionProperty(
            IInvocationOperation invocationOperation,
            Document document,
            IObjectCreationOperation logEntryCreationOperation,
            CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

            var exceptionParameter = GetExceptionParameter(invocationOperation, out var needToFixCatchClause);

            var exceptionAssignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("Exception"),
                exceptionParameter);

            var logEntryCreationExpression = (BaseObjectCreationExpressionSyntax)logEntryCreationOperation.Syntax;
            BaseObjectCreationExpressionSyntax replacementLogEntryCreationExpression;

            if (logEntryCreationOperation.Initializer is not null
                && logEntryCreationOperation.Initializer.Initializers.Length > 0)
            {
                var expressions = logEntryCreationExpression.Initializer!.Expressions.ToList();
                var replaced = false;

                for (var i = 0; i < expressions.Count; i++)
                {
                    if (expressions[i] is AssignmentExpressionSyntax assignment
                        && assignment.Left is IdentifierNameSyntax identifier
                        && identifier.Identifier.Text == "Exception")
                    {
                        expressions[i] = exceptionAssignment;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    var lastAssignment = expressions[expressions.Count - 1];
                    lastAssignment = lastAssignment.WithoutTrivia().WithLeadingTrivia(lastAssignment.GetLeadingTrivia());
                    expressions[expressions.Count - 1] = lastAssignment;

                    expressions.Add(exceptionAssignment);
                }

                replacementLogEntryCreationExpression = logEntryCreationExpression.WithInitializer(
                    logEntryCreationExpression.Initializer.WithExpressions(
                        SyntaxFactory.SeparatedList(expressions)));
            }
            else
            {
                replacementLogEntryCreationExpression = logEntryCreationExpression.WithInitializer(
                    SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(new[] { exceptionAssignment })));
            }


            if (needToFixCatchClause)
            {
                return await FixCatchClause(invocationOperation, document, root, logEntryCreationExpression, replacementLogEntryCreationExpression, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                root = root.ReplaceNode(logEntryCreationExpression, replacementLogEntryCreationExpression);
                return document.WithSyntaxRoot(root);
            }
        }

        private static async Task<Document> PassExceptionToLoggingExtensionMethod(
            IInvocationOperation invocationOperation,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;

            var exceptionParameter = GetExceptionParameter(invocationOperation, out var needToFixCatchClause);

            var invocationExpression = (InvocationExpressionSyntax)invocationOperation.Syntax;
            var arguments = AddExceptionArgument(invocationExpression.ArgumentList.Arguments, exceptionParameter, invocationOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            if (needToFixCatchClause)
            {
                return await FixCatchClause(invocationOperation, document, root, invocationExpression, replacementInvocationExpression, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                root = root.ReplaceNode(invocationExpression, replacementInvocationExpression);
                return document.WithSyntaxRoot(root);
            }
        }

        private static IEnumerable<ArgumentSyntax> AddExceptionArgument(IEnumerable<ArgumentSyntax> argumentsToFix,
            IdentifierNameSyntax exceptionParameter, IEnumerable<IArgumentOperation> argumentOperations)
        {
            var exceptionArgument = SyntaxFactory.Argument(exceptionParameter);
            var arguments = argumentsToFix.ToList();

            if (!arguments.Any(a => a.NameColon is not null))
            {
                if (argumentOperations
                    .FirstOrDefault(a => a.Parameter!.Name == "exception")
                    ?.Syntax is ArgumentSyntax existingExceptionArgument)
                {
                    for (var i = 0; i < arguments.Count; i++)
                    {
                        if (arguments[i] == existingExceptionArgument)
                        {
                            arguments[i] = exceptionArgument;
                            break;
                        }
                    }
                }
                else
                {
                    arguments.Insert(1, exceptionArgument);
                }
            }
            else
            {
                exceptionArgument = exceptionArgument.WithNameColon(SyntaxFactory.NameColon("exception"));

                if (argumentOperations
                    .FirstOrDefault(a => a.Parameter!.Name == "exception")
                    ?.Syntax is ArgumentSyntax existingExceptionArgument)
                {
                    for (var i = 0; i < arguments.Count; i++)
                    {
                        if (arguments[i] == existingExceptionArgument)
                        {
                            arguments[i] = exceptionArgument;
                            break;
                        }
                    }
                }
                else
                {
                    arguments.Add(exceptionArgument);
                }
            }

            return arguments;
        }

        private static IdentifierNameSyntax GetExceptionParameter(IInvocationOperation invocationOperation, out bool needToFixCatchClause)
        {
            var catchClause = GetCatchClause(invocationOperation)!;

            if (catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator)
            {
                needToFixCatchClause = false;
                return SyntaxFactory.IdentifierName(variableDeclarator.Symbol.Name);
            }

            needToFixCatchClause = true;
            return SyntaxFactory.IdentifierName(SyntaxFactory.ParseToken("ex"));
        }

        private static async Task<Document> FixCatchClause(IInvocationOperation invocationOperation, Document document,
            SyntaxNode root, SyntaxNode expression, SyntaxNode replacementExpression, CancellationToken cancellationToken)
        {
            var catchClause = GetCatchClause(invocationOperation.Syntax)!;

            bool checkImportsForSystem;

            var identifier = SyntaxFactory.ParseToken("ex");

            var replacementCatchClause = catchClause.ReplaceNode(expression, replacementExpression);

            TypeSyntax exceptionType;
            if (catchClause.Declaration is null)
            {
                replacementCatchClause = replacementCatchClause
                    .WithCatchKeyword(catchClause.CatchKeyword.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(" ")));

                exceptionType = SyntaxFactory.IdentifierName("Exception");
                checkImportsForSystem = true;
            }
            else
            {
                exceptionType = catchClause.Declaration.Type;
                checkImportsForSystem = false;
            }

            replacementCatchClause = replacementCatchClause
                .WithDeclaration(SyntaxFactory.CatchDeclaration(exceptionType, identifier));

            root = root.ReplaceNode(catchClause, replacementCatchClause);

            if (checkImportsForSystem
                && root is CompilationUnitSyntax compilationUnit
                && !compilationUnit.Usings.Any(u => u.Name.ToFullString() == "System"))
            {
                var name = SyntaxFactory.IdentifierName("System");
                root = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(name));
                return await Formatter.OrganizeImportsAsync(document.WithSyntaxRoot(root), cancellationToken).ConfigureAwait(false);
            }

            return document.WithSyntaxRoot(root);
        }

        private static ICatchClauseOperation? GetCatchClause(IInvocationOperation invocationOperation)
        {
            var parent = invocationOperation.Parent;
            while (parent is not null)
            {
                if (parent is ICatchClauseOperation catchClause)
                {
                    return catchClause;
                }
                parent = parent.Parent;
            }
            return null;
        }

        private static CatchClauseSyntax? GetCatchClause(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent is not null)
            {
                if (parent is CatchClauseSyntax catchClause)
                {
                    return catchClause;
                }
                parent = parent.Parent;
            }
            return null;
        }
    }
}
