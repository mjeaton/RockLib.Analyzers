using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RockLib.Analyzers.Common;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RockLib.Logging.AspNetCore.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddInfoLogAttributeCodeFixProvider)), Shared]
    public class AddInfoLogAttributeCodeFixProvider : CodeFixProvider
    {
        public const string AddInfoLogAttributeTitle = "Add InfoLog attribute";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.AddInfoLogAttribute);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan) as MemberDeclarationSyntax;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: AddInfoLogAttributeTitle,
                        createChangedDocument: cancellationToken =>
                            AddInfoLogAttributeAsync(context.Document, root, node, cancellationToken),
                        equivalenceKey: nameof(AddInfoLogAttributeTitle)),
                    diagnostic);
            }
        }

        private static async Task<Document> AddInfoLogAttributeAsync(Document document, SyntaxNode root, MemberDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var attributes = declaration.AttributeLists.Add(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("InfoLog")))));

            root = root.ReplaceNode(declaration, declaration.WithAttributeLists(attributes));

            if (root is CompilationUnitSyntax compilationUnit
                && !compilationUnit.Usings.Any(u => u.Name.ToFullString() == "RockLib.Logging.AspNetCore"))
            {
                var name = SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("RockLib"),
                        SyntaxFactory.IdentifierName("Logging")),
                    SyntaxFactory.IdentifierName("AspNetCore"));
                root = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(name));
            }

            return document.WithSyntaxRoot(root);
        }
    }
}
