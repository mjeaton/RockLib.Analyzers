using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace RockLib.Logging.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnexpectedExtendedPropertiesCodeFixProvider)), Shared]
    public sealed class UnexpectedExtendedPropertiesCodeFixProvider : CodeFixProvider
    {
        public const string ReplaceWithAnonymousObjectTitle = "Replace with anonymous object";

        public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.UnexpectedExtendedPropertiesObject);

        public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;
            var semanticModel = (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false))!;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                if (node is InvocationExpressionSyntax invocation
                    && invocation.ArgumentList.Arguments.Count > 1)
                {
                    var methodInvocation = (IInvocationOperation)semanticModel.GetOperation(invocation, context.CancellationToken)!;
                    var arg = methodInvocation.Arguments.First(a => a.Parameter!.Name == "extendedProperties");

                    context.RegisterCodeFix(
                       CodeAction.Create(
                       ReplaceWithAnonymousObjectTitle,
                       createChangedDocument: cancellationToken => ChangeDocForLoggingExtension(arg, invocation, methodInvocation, context),
                       equivalenceKey: nameof(ReplaceWithAnonymousObjectTitle)), diagnostic);
                }
                else if (node is ObjectCreationExpressionSyntax objectCreation)
                {
                    var newLogEntryOperation = (IObjectCreationOperation)semanticModel.GetOperation(objectCreation, context.CancellationToken)!;

                    context.RegisterCodeFix(
                        CodeAction.Create(
                        ReplaceWithAnonymousObjectTitle,
                        createChangedDocument: cancellationToken => ChangeDocForLogEntry(objectCreation,newLogEntryOperation, context),
                        equivalenceKey: nameof(ReplaceWithAnonymousObjectTitle)), diagnostic);
                }
            }
        }

        private static async Task<Document> ChangeDocForLogEntry(ObjectCreationExpressionSyntax objectCreation, IObjectCreationOperation newLogEntryOperation, CodeFixContext context)
        {
            var docEditor = await DocumentEditor.CreateAsync(context.Document).ConfigureAwait(false);
            var arg = newLogEntryOperation.Arguments.First(a => a.Parameter!.Name == "extendedProperties");
            var extendedPropertiesArg = (ArgumentSyntax)arg.Syntax;
            var goodArgs = objectCreation.ArgumentList!.Arguments.Where(a => a != extendedPropertiesArg);

            var extendedProps = CreateAnonymousObjectAsArgument(arg, true);

            var invocationExpression = (ObjectCreationExpressionSyntax)newLogEntryOperation.Syntax;
            var arguments = UpdateArguments(objectCreation.ArgumentList.Arguments, extendedProps.First(), newLogEntryOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            docEditor.ReplaceNode(objectCreation, replacementInvocationExpression);

            return docEditor.GetChangedDocument();
        }

        private static async Task<Document> ChangeDocForLoggingExtension(IArgumentOperation arg, InvocationExpressionSyntax invocation, IInvocationOperation invocationOperation, CodeFixContext context)
        {
            var docEditor = await DocumentEditor.CreateAsync(context.Document).ConfigureAwait(false);
            var extendedProps = CreateAnonymousObjectAsArgument(arg, false);
            var invocationExpression = (InvocationExpressionSyntax)invocationOperation.Syntax;
            var arguments = UpdateArguments(invocationExpression.ArgumentList.Arguments, extendedProps.First(), invocationOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            docEditor.ReplaceNode(invocation, replacementInvocationExpression);

            return docEditor.GetChangedDocument();
        }

        private static IEnumerable<ArgumentSyntax> CreateAnonymousObjectAsArgument(IArgumentOperation arg, bool includeNamedColon)
        {
            var anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression();
            if (arg.Value is IConversionOperation conversion
                && conversion.Operand is ILocalReferenceOperation localOperation)
            {
                var anonymousArgumentName = localOperation.Local.Name;
                var anonymousObjectDeclarator = SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.IdentifierName(anonymousArgumentName));
                var anonymousObjectParameter = new List<AnonymousObjectMemberDeclaratorSyntax>() { anonymousObjectDeclarator };
                anonymousObjectCreation = anonymousObjectCreation.WithInitializers(SyntaxFactory.SeparatedList(anonymousObjectParameter));
            }
            else if (arg.Value is IConversionOperation objectConversion
                && objectConversion.Operand is IObjectCreationOperation convertedObj
                && convertedObj.Syntax is BaseObjectCreationExpressionSyntax baseObjectCreation)
            {
                var objectInitializerArgs = baseObjectCreation.ArgumentList;
                var name = convertedObj.Type!.Name;
                anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AnonymousObjectMemberDeclarator(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName(name))
                        .WithArgumentList(objectInitializerArgs))
                    .WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)))));
            }

            var anonymousObjectArgument = includeNamedColon
                 ? SyntaxFactory.Argument(anonymousObjectCreation).WithNameColon(SyntaxFactory.NameColon("extendedProperties"))
                 : SyntaxFactory.Argument(anonymousObjectCreation);

            var extendedPropertyArguments = new List<ArgumentSyntax>();
            extendedPropertyArguments.Add(anonymousObjectArgument);

            return extendedPropertyArguments;
        }

        private static IEnumerable<ArgumentSyntax> UpdateArguments(IEnumerable<ArgumentSyntax> argumentsToFix,
           ArgumentSyntax argumentSyntax, IEnumerable<IArgumentOperation> argumentOperations)
        {
            var arguments = argumentsToFix.ToList();

            if (argumentOperations
                .FirstOrDefault(a => a.Parameter!.Name == "extendedProperties")
                ?.Syntax is ArgumentSyntax existingExceptionArgument)
            {
                for (var i = 0; i < arguments.Count; i++)
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
