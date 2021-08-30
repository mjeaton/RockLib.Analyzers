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
        public const string ReplaceTypeTitle = "Replace type to anonymous object";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.UnexpectedExtendedPropertiesObject);

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
                    var methodInvocation = (IInvocationOperation)semanticModel.GetOperation(invocation);
                    var arg = methodInvocation.Arguments.FirstOrDefault(a => a.Parameter.Name == "extendedProperties");

                    context.RegisterCodeFix(
                       CodeAction.Create(
                       ChangeToAnonymousObjectTitle,
                       createChangedDocument: cancellationToken => ChangeDoc(arg, invocation, methodInvocation, context),
                       equivalenceKey: nameof(ReplaceTypeTitle)), diagnostic);
                }
                else if (node is ObjectCreationExpressionSyntax objectCreation)
                {
                    // var methodInvocation = (IInvocationOperation)semanticModel.GetOperation(invocation);
                    var newLogEntryOperation = (IObjectCreationOperation)semanticModel.GetOperation(objectCreation, context.CancellationToken);



                    context.RegisterCodeFix(
                        CodeAction.Create(
                        ChangeToAnonymousObjectTitle,
                        createChangedDocument: cancellationToken => ChangeDocForLogEntry(objectCreation,newLogEntryOperation, context),
                        equivalenceKey: nameof(ReplaceTypeTitle)), diagnostic);

                }
            }
        }

        private ObjectCreationExpressionSyntax CreateAnonymousObject(IArgumentOperation arg, ObjectCreationExpressionSyntax objectCreation, IObjectCreationOperation invocationOperation)
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
                var name = convertedObj.Type.Name;
                anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression(

                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.AnonymousObjectMemberDeclarator(
                                                SyntaxFactory.ObjectCreationExpression(
                                                    SyntaxFactory.IdentifierName(name))
                                                .WithArgumentList(objectInitializerArgs))
                                            .WithNameEquals(
                                                SyntaxFactory.NameEquals(
                                                    SyntaxFactory.IdentifierName(name)))));
            }
            var extendedPropertyArguments = new List<ArgumentSyntax>();
            extendedPropertyArguments.Add(SyntaxFactory.Argument(anonymousObjectCreation).WithNameColon(SyntaxFactory.NameColon("extendedProperties")));

            var invocationExpression = (ObjectCreationExpressionSyntax)invocationOperation.Syntax;
            var arguments = AddAnonArgument(objectCreation.ArgumentList.Arguments, extendedPropertyArguments[0], invocationOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            return replacementInvocationExpression;
        }

        private async Task<Document> ChangeDocForLogEntry(ObjectCreationExpressionSyntax objectCreation, IObjectCreationOperation newLogEntryOperation, CodeFixContext context)
        {
            var docEditor = await DocumentEditor.CreateAsync(context.Document);
            var arg = newLogEntryOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "extendedProperties");
            var extendedPropertiesArg = (ArgumentSyntax)arg.Syntax;
            var goodArgs = objectCreation.ArgumentList.Arguments.Where(a => a != extendedPropertiesArg);

            docEditor.ReplaceNode(objectCreation, CreateAnonymousObject(arg, objectCreation, newLogEntryOperation));

            return docEditor.GetChangedDocument();
        }

        private async Task<Document> ChangeDoc(IArgumentOperation arg, InvocationExpressionSyntax invocation, IInvocationOperation invocationOperation, CodeFixContext context)
        {
            var docEditor = await DocumentEditor.CreateAsync(context.Document);
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
                && objectConversion.Operand is IObjectCreationOperation objectCreation
                && objectCreation.Syntax is BaseObjectCreationExpressionSyntax baseObjectCreation)
            {
                var objectInitializerArgs = baseObjectCreation.ArgumentList;
                var name = objectCreation.Type.Name;
                anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.AnonymousObjectMemberDeclarator(
                                                SyntaxFactory.ObjectCreationExpression(
                                                    SyntaxFactory.IdentifierName(name))
                                                .WithArgumentList(objectInitializerArgs))
                                            .WithNameEquals(
                                                SyntaxFactory.NameEquals(
                                                    SyntaxFactory.IdentifierName(name)))));
            }

            var extendedPropertyArguments = new List<ArgumentSyntax>();
            extendedPropertyArguments.Add(SyntaxFactory.Argument(anonymousObjectCreation));

            var invocationExpression = (InvocationExpressionSyntax)invocationOperation.Syntax;
            var arguments = AddAnonArgument(invocationExpression.ArgumentList.Arguments, extendedPropertyArguments[0], invocationOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            docEditor.ReplaceNode(invocation, replacementInvocationExpression);

            return docEditor.GetChangedDocument();
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
