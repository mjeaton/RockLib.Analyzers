using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace RockLib.Logging.AspNetCore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AddInfoLogAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Add InfoLog attribute";
        private static readonly LocalizableString _messageFormat = "Add an [InfoLog] attribute to the {0}";
        private static readonly LocalizableString _description = "Add an [InfoLog] attribute to automatically log controller actions.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.AddInfoLogAttribute,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.UseSanitizingLoggingMethod));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null) { throw new ArgumentNullException(nameof(context)); }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var infoLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.AspNetCore.InfoLogAttribute");
            if (infoLogAttributeType is null) 
            { 
                return; 
            }

            var nonControllerAttributeType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.NonControllerAttribute");
            if (nonControllerAttributeType is null)
            {
                return;
            }

            var controllerAttributeType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
            if (controllerAttributeType is null)
            {
                return;
            }

            var nonActionAttributeType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.NonActionAttribute");
            if (nonActionAttributeType is null)
            {
                return;
            }

            var filterCollectionType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Filters.FilterCollection");
            if (filterCollectionType is null)
            {
                return;
            }

            var analyzer = new Analyzer(infoLogAttributeType, nonControllerAttributeType,
                controllerAttributeType, nonActionAttributeType, filterCollectionType);

            context.RegisterSymbolAction(analyzer.AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(analyzer.AnalyzeMethod, SymbolKind.Method);
        }

        private sealed class Analyzer
        {
            private readonly INamedTypeSymbol _infoLogAttributeType;
            private readonly INamedTypeSymbol _nonControllerAttributeType;
            private readonly INamedTypeSymbol _controllerAttributeType;
            private readonly INamedTypeSymbol _nonActionAttributeType;
            private readonly INamedTypeSymbol _filterCollectionType;

            public Analyzer(INamedTypeSymbol infoLogAttributeType, INamedTypeSymbol nonControllerAttributeType,
                INamedTypeSymbol controllerAttributeType, INamedTypeSymbol nonActionAttributeType,
                INamedTypeSymbol filterCollectionType)
            {
                _infoLogAttributeType = infoLogAttributeType;
                _nonControllerAttributeType = nonControllerAttributeType;
                _controllerAttributeType = controllerAttributeType;
                _nonActionAttributeType = nonActionAttributeType;
                _filterCollectionType = filterCollectionType;
            }

            public void AnalyzeType(SymbolAnalysisContext context)
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                if (IsController(namedTypeSymbol))
                {
                    if (HasInfoLogAttribute(namedTypeSymbol)) { return; }

                    foreach (var method in namedTypeSymbol.GetMembers().OfType<IMethodSymbol>())
                    {
                        if (method.MethodKind == MethodKind.Ordinary && HasInfoLogAttribute(method)) { return; }
                    }

                    if (IsInfoLogAttributeAddedToFilterCollection(context)) { return; }

                    var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], $"{namedTypeSymbol.Name} controller");
                    context.ReportDiagnostic(diagnostic);
                }
            }

            public void AnalyzeMethod(SymbolAnalysisContext context)
            {
                var methodSymbol = (IMethodSymbol)context.Symbol;

                var containingType = methodSymbol.ContainingType;

                if (IsController(containingType) && IsAction(methodSymbol))
                {
                    if (methodSymbol.MethodKind == MethodKind.Ordinary
                        && (HasInfoLogAttribute(containingType) || HasInfoLogAttribute(methodSymbol)))
                    {
                        return;
                    }

                    if (IsInfoLogAttributeAddedToFilterCollection(context))
                    {
                        return;
                    }

                    var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], $"{methodSymbol.Name} action method");
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private bool HasInfoLogAttribute(ISymbol symbol) => IsAttributeDefined(symbol, _infoLogAttributeType);

            private static bool IsAttributeDefined(ISymbol symbol, INamedTypeSymbol attributeType) =>
                symbol.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));

            private bool IsInfoLogAttributeAddedToFilterCollection(SymbolAnalysisContext context)
            {
                var visitor = new SyntaxWalker(_filterCollectionType, _infoLogAttributeType,
                    context.Compilation, context.CancellationToken);
                return visitor.IsInfoLogAttributeAddedToFilterCollection;
            }

            // https://github.com/dotnet/aspnetcore/blob/62b77d0bad02e72b6675545392ecb3232d508e43/src/Mvc/Mvc.Core/src/Controllers/ControllerFeatureProvider.cs#L41
            private bool IsController(INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsValueType ||  namedTypeSymbol.IsAbstract ||
                    namedTypeSymbol.DeclaredAccessibility != Accessibility.Public ||
                    namedTypeSymbol.IsGenericType || IsAttributeDefined(namedTypeSymbol, _nonControllerAttributeType) ||
                    (!namedTypeSymbol.Name.EndsWith("Controller", StringComparison.InvariantCulture) && 
                        !IsAttributeDefined(namedTypeSymbol, _controllerAttributeType)))
                {
                    return false;
                }

                return true;
            }

            // https://github.com/dotnet/aspnetcore/blob/62b77d0bad02e72b6675545392ecb3232d508e43/src/Mvc/Mvc.Core/src/ApplicationModels/DefaultApplicationModelProvider.cs#L396
            private bool IsAction(IMethodSymbol methodSymbol)
            {
                if (methodSymbol.IsStatic || methodSymbol.IsAbstract ||
                    methodSymbol.MethodKind == MethodKind.Constructor ||
                    methodSymbol.IsGenericMethod ||
                    methodSymbol.DeclaredAccessibility != Accessibility.Public ||
                    IsAttributeDefined(methodSymbol, _nonActionAttributeType) ||
                    methodSymbol.OverriddenMethod?.ContainingType.SpecialType == SpecialType.System_Object)
                {
                    return false;
                }

                return true;
            }

            private sealed class SyntaxWalker : CSharpSyntaxWalker
            {
                private readonly INamedTypeSymbol _filterCollectionType;
                private readonly INamedTypeSymbol _infoLogAttributeType;
                private readonly CancellationToken _cancellationToken;
                private readonly Compilation _compilation;

                public SyntaxWalker(INamedTypeSymbol filterCollectionType,
                    INamedTypeSymbol infoLogAttributeType, Compilation compilation, CancellationToken cancellationToken)
                {
                    _filterCollectionType = filterCollectionType;
                    _infoLogAttributeType = infoLogAttributeType;
                    _cancellationToken = cancellationToken;
                    _compilation = compilation;

                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        Visit(syntaxTree.GetRoot(_cancellationToken));
                    }
                }

                public bool IsInfoLogAttributeAddedToFilterCollection { get; private set; }

                public override void VisitInvocationExpression(InvocationExpressionSyntax node)
                {
                    if (node.Expression is MemberAccessExpressionSyntax memberAccess
                        && (memberAccess.Name.Identifier.Text == "Add" || memberAccess.Name.Identifier.Text == "AddService")
                        && memberAccess.Expression is MemberAccessExpressionSyntax memberAccess2
                        && memberAccess2.Name.Identifier.Text == "Filters"
                        && _compilation.GetSemanticModel(node.SyntaxTree) is SemanticModel semanticModel
                        && semanticModel.GetOperation(node, _cancellationToken) is IInvocationOperation invocation
                        && SymbolEqualityComparer.Default.Equals(invocation.Instance!.Type, _filterCollectionType))
                    {
                        if (invocation.TargetMethod.IsGenericMethod)
                        {
                            if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.TypeArguments[0], _infoLogAttributeType))
                                IsInfoLogAttributeAddedToFilterCollection = true;
                        }
                        else
                        {
                            if (invocation.Arguments[0].Value is ITypeOfOperation typeOfOperation
                                && SymbolEqualityComparer.Default.Equals(typeOfOperation.TypeOperand, _infoLogAttributeType))
                            {
                                IsInfoLogAttributeAddedToFilterCollection = true;
                            }
                        }
                    }

                    base.VisitInvocationExpression(node);
                }
            }
        }
    }
}
