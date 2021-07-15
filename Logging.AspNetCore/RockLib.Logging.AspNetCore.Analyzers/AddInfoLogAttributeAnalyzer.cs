﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RockLib.Analyzers.Common;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.AspNetCore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddInfoLogAttributeAnalyzer : DiagnosticAnalyzer
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
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.UseSanitizingLoggingMethod));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var infoLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.AspNetCore.InfoLogAttribute");
            if (infoLogAttributeType == null)
                return;

            var nonControllerAttributeType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.NonControllerAttribute");
            if (nonControllerAttributeType == null)
                return;

            var controllerAttributeType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
            if (controllerAttributeType == null)
                return;

            var nonActionAttributeType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.NonActionAttribute");
            if (nonActionAttributeType == null)
                return;

            var analyzer = new Analyzer(infoLogAttributeType, nonControllerAttributeType, controllerAttributeType, nonActionAttributeType);

            context.RegisterSymbolAction(analyzer.AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(analyzer.AnalyzeMethod, SymbolKind.Method);
        }

        private class Analyzer
        {
            private readonly INamedTypeSymbol _infoLogAttributeType;
            private readonly INamedTypeSymbol _nonControllerAttributeType;
            private readonly INamedTypeSymbol _controllerAttributeType;
            private readonly INamedTypeSymbol _nonActionAttributeType;

            public Analyzer(INamedTypeSymbol infoLogAttributeType, INamedTypeSymbol nonControllerAttributeType,
                INamedTypeSymbol controllerAttributeType, INamedTypeSymbol nonActionAttributeType)
            {
                _infoLogAttributeType = infoLogAttributeType;
                _nonControllerAttributeType = nonControllerAttributeType;
                _controllerAttributeType = controllerAttributeType;
                _nonActionAttributeType = nonActionAttributeType;
            }

            public void AnalyzeType(SymbolAnalysisContext context)
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                if (IsController(namedTypeSymbol))
                {
                    if (HasInfoLogAttribute(namedTypeSymbol))
                        return;

                    foreach (var method in namedTypeSymbol.GetMembers().OfType<IMethodSymbol>())
                        if (method.MethodKind == MethodKind.Ordinary && HasInfoLogAttribute(method))
                            return;

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
                        return;

                    var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], $"{methodSymbol.Name} action method");
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private bool HasInfoLogAttribute(ISymbol symbol) => IsAttributeDefined(symbol, _infoLogAttributeType);

            private static bool IsAttributeDefined(ISymbol symbol, INamedTypeSymbol attributeType) =>
                symbol.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));

            // https://github.com/dotnet/aspnetcore/blob/62b77d0bad02e72b6675545392ecb3232d508e43/src/Mvc/Mvc.Core/src/Controllers/ControllerFeatureProvider.cs#L41
            private bool IsController(INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsValueType)
                    return false;

                if (namedTypeSymbol.IsAbstract)
                    return false;

                if (namedTypeSymbol.DeclaredAccessibility != Accessibility.Public)
                    return false;

                if (namedTypeSymbol.IsGenericType)
                    return false;

                if (IsAttributeDefined(namedTypeSymbol, _nonControllerAttributeType))
                    return false;

                if (!namedTypeSymbol.Name.EndsWith("Controller")
                    && !IsAttributeDefined(namedTypeSymbol, _controllerAttributeType))
                    return false;
                
                return true;
            }

            // https://github.com/dotnet/aspnetcore/blob/62b77d0bad02e72b6675545392ecb3232d508e43/src/Mvc/Mvc.Core/src/ApplicationModels/DefaultApplicationModelProvider.cs#L396
            private bool IsAction(IMethodSymbol methodSymbol)
            {
                if (methodSymbol.IsStatic)
                    return false;

                if (methodSymbol.IsAbstract)
                    return false;

                if (methodSymbol.MethodKind == MethodKind.Constructor)
                    return false;

                if (methodSymbol.IsGenericMethod)
                    return false;

                if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
                    return false;

                if (IsAttributeDefined(methodSymbol, _nonActionAttributeType))
                    return false;

                // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
                if (methodSymbol.OverriddenMethod?.ContainingType.SpecialType == SpecialType.System_Object)
                    return false;

                // Dispose method implemented from IDisposable is not valid

                return true;
            }
        }
    }
}
