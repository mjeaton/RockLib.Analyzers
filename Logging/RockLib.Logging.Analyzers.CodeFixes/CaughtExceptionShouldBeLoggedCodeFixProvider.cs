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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CaughtExceptionShouldBeLoggedCodeFixProvider)), Shared]
    public class CaughtExceptionShouldBeLoggedCodeFixProvider : CodeFixProvider
    {
        public const string PassExceptionToLogEntryConstructorTitle = "Pass exception to LogEntry constructor";
        public const string SetLogEntryExceptionPropertyTitle = "Set LogEntry.Exception property";
        public const string PassExceptionToLoggingExtensionMethodTitle = "Pass exception to logging extension method";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.CaughtExceptionShouldBeLogged);

        public sealed override FixAllProvider GetFixAllProvider() => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                var invocationOperation = (IInvocationOperation)semanticModel.GetOperation(node);

                if (invocationOperation.TargetMethod.Name == "Log")
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = GetLogEntryCreationOperation(logEntryArgument);

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
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var exceptionParameter = GetExceptionParameter(invocationOperation, out var needToFixCatchBlock);

            var exceptionArgument = SyntaxFactory.Argument(exceptionParameter);
            var catchClause = GetCatchClause(invocationOperation.Syntax);

            var logEntryCreationExpression = (BaseObjectCreationExpressionSyntax)logEntryCreationOperation.Syntax;
            var arguments = logEntryCreationExpression.ArgumentList.Arguments.ToList();

            if (logEntryCreationOperation.Arguments
                .FirstOrDefault(a => a.Parameter.Name == "exception")
                ?.Syntax is ArgumentSyntax existingExceptionArgument)
            {
                for (int i = 0; i < arguments.Count; i++)
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

            var replacementLogEntryCreationExpression = logEntryCreationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            if (needToFixCatchBlock)
            {
                root = FixCatchClause(root, catchClause, logEntryCreationExpression, replacementLogEntryCreationExpression);
            }
            else
            {
                root = root.ReplaceNode(logEntryCreationExpression, replacementLogEntryCreationExpression);
            }

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> SetLogEntryExceptionProperty(
            IInvocationOperation invocationOperation,
            Document document,
            IObjectCreationOperation logEntryCreationOperation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var exceptionParameter = GetExceptionParameter(invocationOperation, out var needToFixCatchBlock);
            var catchClause = GetCatchClause(invocationOperation.Syntax);

            var exceptionAssignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("Exception"),
                exceptionParameter);

            var logEntryCreationExpression = (BaseObjectCreationExpressionSyntax)logEntryCreationOperation.Syntax;
            BaseObjectCreationExpressionSyntax replacementLogEntryCreationExpression;

            if (logEntryCreationOperation.Initializer != null
                && logEntryCreationOperation.Initializer.Initializers.Length > 0)
            {
                var expressions = logEntryCreationExpression.Initializer.Expressions.ToList();
                var replaced = false;

                for (int i = 0; i < expressions.Count; i++)
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


            if (needToFixCatchBlock)
            {
                root = FixCatchClause(root, catchClause, logEntryCreationExpression, replacementLogEntryCreationExpression);
            }
            else
            {
                root = root.ReplaceNode(logEntryCreationExpression, replacementLogEntryCreationExpression);
            }

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> PassExceptionToLoggingExtensionMethod(
            IInvocationOperation invocationOperation,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var exceptionParameter = GetExceptionParameter(invocationOperation, out var needToFixCatchBlock);
            var catchClause = GetCatchClause(invocationOperation.Syntax);

            var exceptionArgument = SyntaxFactory.Argument(exceptionParameter);

            var invocationExpression = (InvocationExpressionSyntax)invocationOperation.Syntax;
            var arguments = invocationExpression.ArgumentList.Arguments.ToList();

            if (invocationOperation.Arguments
                .FirstOrDefault(a => a.Parameter.Name == "exception")
                ?.Syntax is ArgumentSyntax existingExceptionArgument)
            {
                for (int i = 0; i < arguments.Count; i++)
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

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            if (needToFixCatchBlock)
            {
                root = FixCatchClause(root, catchClause, invocationExpression, replacementInvocationExpression);
            }
            else
            {
                root = root.ReplaceNode(invocationExpression, replacementInvocationExpression);
            }

            return document.WithSyntaxRoot(root);
        }

        private static IdentifierNameSyntax GetExceptionParameter(IInvocationOperation invocationOperation, out bool needToFixCatchBlock)
        {
            var catchClause = GetCatchClause(invocationOperation);

            if (catchClause.ExceptionDeclarationOrExpression is IVariableDeclaratorOperation variableDeclarator)
            {
                needToFixCatchBlock = false;
                return SyntaxFactory.IdentifierName(variableDeclarator.Symbol.Name);
            }

            needToFixCatchBlock = true;
            return SyntaxFactory.IdentifierName(SyntaxFactory.ParseToken("ex"));
        }

        private static SyntaxNode FixCatchClause(SyntaxNode root, CatchClauseSyntax catchClause,
            SyntaxNode expression, SyntaxNode replacementExpression)
        {
            var identifier = SyntaxFactory.ParseToken("ex");

            var replacementCatchClause = catchClause.ReplaceNode(expression, replacementExpression);

            TypeSyntax exceptionType;
            if (catchClause.Declaration == null)
            {
                replacementCatchClause = replacementCatchClause
                    .WithCatchKeyword(catchClause.CatchKeyword.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(" ")));

                exceptionType = SyntaxFactory.IdentifierName("Exception");
            }
            else
                exceptionType = catchClause.Declaration.Type;

            replacementCatchClause = replacementCatchClause
                .WithDeclaration(SyntaxFactory.CatchDeclaration(exceptionType, identifier));

            root = root.ReplaceNode(catchClause, replacementCatchClause);
            return root;
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

        private static ICatchClauseOperation GetCatchClause(IInvocationOperation invocationOperation)
        {
            var parent = invocationOperation.Parent;
            while (parent != null)
            {
                if (parent is ICatchClauseOperation catchClause)
                    return catchClause;
                parent = parent.Parent;
            }
            return null;
        }

        private static CatchClauseSyntax GetCatchClause(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is CatchClauseSyntax catchClause)
                    return catchClause;
                parent = parent.Parent;
            }
            return null;
        }
    }
}
