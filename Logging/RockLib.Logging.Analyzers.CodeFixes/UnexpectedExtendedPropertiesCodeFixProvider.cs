using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RockLib.Logging.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnexpectedExtendedPropertiesCodeFixProvider)), Shared]
    public class UnexpectedExtendedPropertiesCodeFixProvider : CodeFixProvider
    {
        public const string ChangeToAnonymousObjectTitle = "Change to anonymous object";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.UnexpectedExtendedPropertiesObject);

        private static SyntaxNodeOrToken GetNewSyntaxListItem(SyntaxNodeOrToken item)
        {
            if (!item.IsNode)
            {
                return item;
            }

            var member = (AnonymousObjectMemberDeclaratorSyntax)item.AsNode();
            var identifier = member.Expression as IdentifierNameSyntax;
            if (identifier != null &&
                identifier.Identifier.ValueText == member.NameEquals.Name.Identifier.ValueText)
            {
                return SyntaxFactory.AnonymousObjectMemberDeclarator(member.Expression).WithTriviaFrom(member);
            }

            return item;

        }
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                if (node is InvocationExpressionSyntax invocation
                    && invocation.ArgumentList.Arguments.Count > 1)
                {
                    var nonAnon = invocation.ArgumentList.Arguments[1];
                    var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                    var simple = memberAccess.Name;

                    var methodInvocation = (IInvocationOperation)semanticModel.GetOperation(invocation);
                    var arg = methodInvocation.Arguments.FirstOrDefault(a => a.Parameter.Name == "extendedProperties");
                    var extendedPropertiesArg = (ArgumentSyntax)arg.Syntax;

                    var diagnosticSpan = diagnostic.Location.SourceSpan;
                    var nameEquals = root.FindNode(diagnosticSpan) as NameEqualsSyntax;

                    context.RegisterCodeFix(
                       CodeAction.Create(
                       ChangeToAnonymousObjectTitle,
                       createChangedDocument: cancellationToken => ChangeDoc(arg, invocation, methodInvocation, root, context),
                       equivalenceKey: "somekey"), diagnostic);
                }
            }
        }

        private async Task<Document> ChangeDoc(IArgumentOperation arg, InvocationExpressionSyntax invocation, IInvocationOperation invocationOperation, SyntaxNode root, CodeFixContext context)
        {
            var docEditor = await DocumentEditor.CreateAsync(context.Document);
            var anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression();

            var nodes = new List<SyntaxNode>();
            string anonymousArgumentName = string.Empty;
            LocalDeclarationStatementSyntax objectDeclaration = null;
            if (arg.Value is IConversionOperation conversion
                && conversion.Operand is ILocalReferenceOperation localOperation)
            {
                anonymousArgumentName = localOperation.Local.Name;
            }
            else if (arg.Value is IConversionOperation objectConversion
                && objectConversion.Operand is IObjectCreationOperation objectCreation
                && objectCreation.Syntax is BaseObjectCreationExpressionSyntax baseObjectCreation)
            {
                var objectInitializerArgs = baseObjectCreation.ArgumentList;

                var name = objectCreation.Type.Name;
                anonymousArgumentName = char.ToLowerInvariant(name[0]) + name.Substring(1);
                objectDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName(
                        SyntaxFactory.Identifier(
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.VarKeyword,
                            "var",
                            "var",
                            SyntaxFactory.TriviaList())))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(anonymousArgumentName))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(name))
                                .WithArgumentList(
                                    objectInitializerArgs))))))
                    //.WithTriviaFrom(invocation)
                    //.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                    .NormalizeWhitespace();

                nodes.Add(objectDeclaration);
            }

            var ass2 = SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.IdentifierName(anonymousArgumentName));

            var anonymousObjectParameter = new List<AnonymousObjectMemberDeclaratorSyntax>() { ass2 };

            anonymousObjectCreation = anonymousObjectCreation.WithInitializers(SyntaxFactory.SeparatedList(anonymousObjectParameter));

            var extendedPropertyArguments = new List<ArgumentSyntax>();
            extendedPropertyArguments.Add(SyntaxFactory.Argument(anonymousObjectCreation));

            var invocationExpression = (InvocationExpressionSyntax)invocationOperation.Syntax;
            var arguments = AddAnonArgument(invocationExpression.ArgumentList.Arguments, extendedPropertyArguments[0], invocationOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));


            //var newRoot = root.ReplaceNode(
            //        invocation, replacementInvocationExpression)
            //    .InsertNodesBefore(root.FindNode(invocation.Span), nodes);

            docEditor.InsertBefore(invocation, nodes[0]);
            //docEditor.ReplaceNode(
            //        invocation, replacementInvocationExpression);
            // docEditor.InsertBefore(invocation, nodes[0]);

            if (nodes.Any())
            {
                //var found = newRoot.FindNode(invocation.Span);
                //newRoot = newRoot.InsertNodesBefore(found, nodes);

                //var found = root.FindNode(replacementInvocationExpression.Span);
                //newRoot = root.ReplaceNode(found, nodes[0]);

                //var found = newRoot.FindNode(invocation.Span);
                //newRoot = newRoot.InsertNodesBefore(found, nodes);
            }

            //return context.Document.WithSyntaxRoot(newRoot);
            return docEditor.GetChangedDocument();
            return context.Document.WithSyntaxRoot(root);

            // return docEditor.GetChangedDocument();
        }

        private IEnumerable<ArgumentSyntax> AddAnonArgument(IEnumerable<ArgumentSyntax> argumentsToFix,
           ArgumentSyntax argumentSyntax, IEnumerable<IArgumentOperation> argumentOperations)
        {
            var arguments = argumentsToFix.ToList();

            if (argumentOperations
                .FirstOrDefault(a => a.Parameter.Name == "extendedProperties")
                ?.Syntax is ArgumentSyntax existingExceptionArgument)
            {
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (arguments[i] == existingExceptionArgument)
                    {
                        arguments[i] = argumentSyntax;
                        break;
                    }
                }
            }
            else
            {
                arguments.Add(argumentSyntax);
            }

            return arguments;
        }
    }
}
