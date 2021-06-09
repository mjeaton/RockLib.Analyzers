using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RockLib.Logging.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseSanitizingLoggingMethodCodeFixProvider)), Shared]
    public class UseSanitizingLoggingMethodCodeFixProvider : CodeFixProvider
    {
        public const string ChangeToSetSanitizedExtendedPropertiesTitle = "Change to SetSanitizedExtendedProperties";
        public const string ChangeToSanitizingLoggingExtensionMethodTitle = "Change to sanitizing logging extension method";
        public const string ReplaceExtendedPropertiesParameterWithCallToSetSanitizedExtendedPropertiesMethodTitle = "Replace extendedProperties parameter with call to SetSanitizedExtendedProperties method";
        public const string ReplaceWithCallToSetSanitizedExtendedProperty = "Replace with call to SetSanitizedExtendedProperty";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.UseSanitizingLoggingMethod);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);

                if (node is InvocationExpressionSyntax invocation
                    && invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    switch (memberAccess.Name.Identifier.Text)
                    {
                        case "SetExtendedProperties":
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    title: ChangeToSetSanitizedExtendedPropertiesTitle,
                                    createChangedDocument: cancellationToken =>
                                        ChangeToSetSanitizedExtendedPropertiesAsync(
                                            context.Document, memberAccess.Name, cancellationToken),
                                    equivalenceKey: nameof(ChangeToSetSanitizedExtendedPropertiesTitle)),
                                diagnostic);
                            break;
                        case "Debug":
                        case "Info":
                        case "Warn":
                        case "Error":
                        case "Fatal":
                        case "Audit":
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    title: ChangeToSanitizingLoggingExtensionMethodTitle,
                                    createChangedDocument: cancellationToken =>
                                        ChangeToSanitizingLoggingExtensionMethodAsync(
                                            context.Document, memberAccess.Name, cancellationToken),
                                    equivalenceKey: nameof(ChangeToSanitizingLoggingExtensionMethodTitle)),
                                diagnostic);
                            break;
                    }
                }
                else if (node is ObjectCreationExpressionSyntax objectCreation)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: ReplaceExtendedPropertiesParameterWithCallToSetSanitizedExtendedPropertiesMethodTitle,
                            createChangedDocument: cancellationToken =>
                                ReplaceExtendedPropertiesParameterWithCallToSetSanitizedExtendedPropertiesMethod(
                                    context.Document, objectCreation, cancellationToken),
                            equivalenceKey: nameof(ReplaceExtendedPropertiesParameterWithCallToSetSanitizedExtendedPropertiesMethodTitle)),
                        diagnostic);
                }
                else if (node is AssignmentExpressionSyntax assignment)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: ReplaceWithCallToSetSanitizedExtendedProperty,
                            createChangedDocument: cancellationToken =>
                                ChangeToSetSanitizedExtendedPropertyAsync(
                                    context.Document, assignment, cancellationToken),
                            equivalenceKey: nameof(ReplaceWithCallToSetSanitizedExtendedProperty)),
                        diagnostic);
                }
            }
        }

        private static async Task<Document> ChangeToSetSanitizedExtendedPropertiesAsync(
            Document document,
            SimpleNameSyntax setExtendedPropertiesName,
            CancellationToken cancellationToken)
        {
            var replacementIdentifier = SyntaxFactory.Identifier("SetSanitizedExtendedProperties")
                .WithTriviaFrom(setExtendedPropertiesName.Identifier);

            var setSanitizedExtendedPropertiesName = setExtendedPropertiesName.ReplaceToken(
                setExtendedPropertiesName.Identifier, replacementIdentifier);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            root = root.ReplaceNode(setExtendedPropertiesName, setSanitizedExtendedPropertiesName);

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> ChangeToSanitizingLoggingExtensionMethodAsync(
            Document document,
            SimpleNameSyntax loggingExtensionMethodName,
            CancellationToken cancellationToken)
        {
            var replacementIdentifier = SyntaxFactory.Identifier(loggingExtensionMethodName.Identifier.Text + "Sanitized")
                .WithTriviaFrom(loggingExtensionMethodName.Identifier);

            var sanitizedLoggingExtensionMethodName = loggingExtensionMethodName.ReplaceToken(
                loggingExtensionMethodName.Identifier, replacementIdentifier);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            root = root.ReplaceNode(loggingExtensionMethodName, sanitizedLoggingExtensionMethodName);

            if (root is CompilationUnitSyntax compilationUnit
                && !compilationUnit.Usings.Any(u => u.Name.ToFullString() == "RockLib.Logging.SafeLogging"))
            {
                var name = SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("RockLib"),
                        SyntaxFactory.IdentifierName("Logging")),
                    SyntaxFactory.IdentifierName("SafeLogging"));
                root = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(name));
            }

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> ReplaceExtendedPropertiesParameterWithCallToSetSanitizedExtendedPropertiesMethod(
            Document document,
            ObjectCreationExpressionSyntax objectCreation,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var objectCreationOperation = (IObjectCreationOperation)semanticModel.GetOperation(objectCreation);
            var arg = objectCreationOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "extendedProperties");
            var extendedPropertiesArg = (ArgumentSyntax)arg.Syntax;

            var args = objectCreation.ArgumentList.Arguments.Where(a => a != extendedPropertiesArg);

            var replacementObjectCreation = SyntaxFactory.ObjectCreationExpression(
                objectCreation.Type,
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args)),
                objectCreation.Initializer);

            var replacementInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    replacementObjectCreation,
                    SyntaxFactory.IdentifierName("SetSanitizedExtendedProperties")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { extendedPropertiesArg.WithNameColon(null) })));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            root = root.ReplaceNode(objectCreation, replacementInvocation);

            return document.WithSyntaxRoot(root);
        }

        private static async Task<Document> ChangeToSetSanitizedExtendedPropertyAsync(
            Document document,
            AssignmentExpressionSyntax assignment,
            CancellationToken cancellationToken)
        {
            var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
            var memberAccess = (MemberAccessExpressionSyntax)elementAccess.Expression;

            var target = memberAccess.Expression;
            var propertyNameArgument = elementAccess.ArgumentList.Arguments[0];
            var valueArgument = SyntaxFactory.Argument(assignment.Right);

            var replacementInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    target,
                    SyntaxFactory.IdentifierName("SetSanitizedExtendedProperty")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[] { propertyNameArgument, valueArgument })));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            root = root.ReplaceNode(assignment, replacementInvocation);

            return document.WithSyntaxRoot(root);
        }
    }
}
