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
        private static readonly LocalizableString _messageFormat = "Add an InfoLog attribute to the {0} controller";
        private static readonly LocalizableString _description = "Add an InfoLog attribute to the controller to automatically controller actions.";

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
            var controllerBaseType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
            if (controllerBaseType == null)
                return;

            var infoLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.AspNetCore.InfoLogAttribute");
            if (infoLogAttributeType == null)
                return;

            var analyzer = new Analyzer(controllerBaseType, infoLogAttributeType);

            context.RegisterSymbolAction(analyzer.AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(analyzer.AnalyzeMethod, SymbolKind.Method);
        }

        private class Analyzer
        {
            private readonly INamedTypeSymbol _controllerBaseType;
            private readonly INamedTypeSymbol _infoLogAttributeType;

            public Analyzer(INamedTypeSymbol controllerBaseType, INamedTypeSymbol infoLogAttributeType)
            {
                _controllerBaseType = controllerBaseType;
                _infoLogAttributeType = infoLogAttributeType;
            }

            public void AnalyzeType(SymbolAnalysisContext context)
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                if (IsControllerBase(namedTypeSymbol))
                {
                    if (HasInfoLogAttribute(namedTypeSymbol))
                        return;

                    foreach (var method in namedTypeSymbol.GetMembers().OfType<IMethodSymbol>())
                        if (method.MethodKind == MethodKind.Ordinary && HasInfoLogAttribute(method))
                            return;

                    var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            public void AnalyzeMethod(SymbolAnalysisContext context)
            {
                var methodSymbol = (IMethodSymbol)context.Symbol;

                var containingType = methodSymbol.ContainingType;

                if (IsControllerBase(containingType))
                {
                    if (methodSymbol.MethodKind == MethodKind.Ordinary
                        && (HasInfoLogAttribute(containingType) || HasInfoLogAttribute(methodSymbol)))
                        return;

                    var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private bool HasInfoLogAttribute(ISymbol symbol) => 
                symbol.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _infoLogAttributeType));

            private bool IsControllerBase(INamedTypeSymbol namedTypeSymbol)
            {
                if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, _controllerBaseType))
                    return true;
                if (namedTypeSymbol.BaseType == null)
                    return false;
                return IsControllerBase(namedTypeSymbol.BaseType);
            }
        }
    }
}
