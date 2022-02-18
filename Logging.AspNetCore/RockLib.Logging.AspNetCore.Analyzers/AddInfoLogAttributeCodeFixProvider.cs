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
    public sealed class AddInfoLogAttributeCodeFixProvider : CodeFixProvider
    {
        public const string AddInfoLogAttributeTitle = "Add InfoLog attribute";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.AddInfoLogAttribute);

        public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))!;

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = (MemberDeclarationSyntax)root.FindNode(diagnostic.Location.SourceSpan);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: AddInfoLogAttributeTitle,
                        createChangedDocument: cancellationToken =>
                            AddInfoLogAttributeAsync(context.Document, root, node),
                        equivalenceKey: nameof(AddInfoLogAttributeTitle)),
                    diagnostic);
            }
        }

        private static Task<Document> AddInfoLogAttributeAsync(Document document, SyntaxNode root, MemberDeclarationSyntax declaration)
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

            return Task.FromResult(document.WithSyntaxRoot(root));
        }
    }
}
