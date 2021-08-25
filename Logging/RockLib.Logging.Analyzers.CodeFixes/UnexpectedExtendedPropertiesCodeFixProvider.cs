using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        private Task<Document> ChangeDoc(IArgumentOperation arg, InvocationExpressionSyntax invocation, IInvocationOperation invocationOperation, SyntaxNode root, CodeFixContext context)
        {
            var anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression();

            var operation = arg.Value as IConversionOperation;
            var operationName = operation.Operand as ILocalReferenceOperation;
            var argName = operationName.Local.Name;

            var ass2 = SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.IdentifierName(argName));

            var anonymousObjectParameter = new List<AnonymousObjectMemberDeclaratorSyntax>() { ass2 };

            anonymousObjectCreation = anonymousObjectCreation.WithInitializers(SyntaxFactory.SeparatedList(anonymousObjectParameter));

            var extendedPropertyArguments = new List<ArgumentSyntax>();
            extendedPropertyArguments.Add(SyntaxFactory.Argument(anonymousObjectCreation));

            var invocationExpression = (InvocationExpressionSyntax)invocationOperation.Syntax;
            var arguments = AddAnonArgument(invocationExpression.ArgumentList.Arguments, extendedPropertyArguments[0], invocationOperation.Arguments);

            var replacementInvocationExpression = invocationExpression.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            var newRoot = root.ReplaceNode(
                invocation,
                replacementInvocationExpression);

            return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
        }

        private class AnonymousObjectWalker : OperationWalker
        {
            private readonly INamedTypeSymbol _objectType;
            private readonly ILocalReferenceOperation _objectReference;

            public AnonymousObjectWalker(INamedTypeSymbol objectType, ILocalReferenceOperation objectReference)
            {
                _objectType = objectType;
                _objectReference = objectReference;
            }

            public string ObjectSymbol{ get; private set; }

            public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
            {
                if (operation.Target is IPropertyReferenceOperation property
                       && SymbolEqualityComparer.Default.Equals(property.Type, _objectType)
                       && property.Instance is ILocalReferenceOperation localReference
                       && SymbolEqualityComparer.Default.Equals(localReference.Local, _objectReference.Local))
                {
                    ObjectSymbol = localReference.Local.Name;
                }

                base.VisitSimpleAssignment(operation);base.VisitSimpleAssignment(operation);
            }
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
