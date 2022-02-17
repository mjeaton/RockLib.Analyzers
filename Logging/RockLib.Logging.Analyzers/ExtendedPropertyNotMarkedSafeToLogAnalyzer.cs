using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExtendedPropertyNotMarkedSafeToLogAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Extended property not marked as safe to log";
        private static readonly LocalizableString _messageFormat = "The '{0}' type does not have any properties marked as safe to log";
        private static readonly LocalizableString _description = "The value of a sanitized extended property should have a type with one or more properties decorated with the [SafeToLog] or else be decorated with the [SafeToLog] attribute itself.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.ExtendedPropertyNotMarkedSafeToLog,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.ExtendedPropertyNotMarkedSafeToLog));

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
            var logEntryType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
            if (logEntryType is null) { return; }

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType is null) { return; }

            var safeToLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeToLogAttribute");
            if (safeToLogAttributeType is null) { return; }

            var notSafeToLogAttributeType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.NotSafeToLogAttribute");
            if (notSafeToLogAttributeType is null) { return; }

            var analyzer = new InvocationOperationAnalyzer(logEntryType, safeLoggingExtensionsType,
                safeToLogAttributeType, notSafeToLogAttributeType, context.Compilation, context.CancellationToken);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private sealed class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _safeLoggingExtensionsType;
            private readonly INamedTypeSymbol _safeToLogAttributeType;
            private readonly INamedTypeSymbol _notSafeToLogAttributeType;
            private readonly Compilation _compilation;
            private readonly CancellationToken _cancellationToken;

            public InvocationOperationAnalyzer(INamedTypeSymbol logEntryType, INamedTypeSymbol safeLoggingExtensionsType,
                INamedTypeSymbol safeToLogAttributeType, INamedTypeSymbol notSafeToLogAttributeType,
                Compilation compilation, CancellationToken cancellationToken)
            {
                _logEntryType = logEntryType;
                _safeLoggingExtensionsType = safeLoggingExtensionsType;
                _safeToLogAttributeType = safeToLogAttributeType;
                _notSafeToLogAttributeType = notSafeToLogAttributeType;
                _compilation = compilation;
                _cancellationToken = cancellationToken;
            }

            public void Analyze(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                {
                    return;
                }

                if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _safeLoggingExtensionsType))
                {
                    AnalyzeExtendedPropertiesArgument(context, invocationOperation);
                }
                else if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _logEntryType))
                {
                    if (methodSymbol.Name == "SetSanitizedExtendedProperty")
                    {
                        AnalyzeSetSanitizedExtendedPropertyMethodCall(context, invocationOperation);
                    }
                    else if (methodSymbol.Name == "SetSanitizedExtendedProperties")
                    {
                        AnalyzeExtendedPropertiesArgument(context, invocationOperation);
                    }
                }
            }

            private void AnalyzeSetSanitizedExtendedPropertyMethodCall(OperationAnalysisContext context,
                IInvocationOperation invocationOperation)
            {
                var valueArgument = invocationOperation.Arguments[1];
                if (valueArgument.Value is IConversionOperation convertToObjectType
                    && convertToObjectType.Type!.SpecialType == SpecialType.System_Object)
                {
                    AnalyzePropertyValue(convertToObjectType.Operand, context.ReportDiagnostic);
                }
            }

            private void AnalyzeExtendedPropertiesArgument(OperationAnalysisContext context, IInvocationOperation invocationOperation)
            {
                var extendedPropertiesArgument = invocationOperation.Arguments
                        .FirstOrDefault(argument => argument.Parameter!.Name == "extendedProperties");

                if (extendedPropertiesArgument is null
                    || !(extendedPropertiesArgument.Value is IConversionOperation convertToObjectType)
                    || convertToObjectType.Type!.SpecialType != SpecialType.System_Object)
                {
                    return;
                }

                var extendedPropertiesArgumentValue = convertToObjectType.Operand;

                if (extendedPropertiesArgumentValue.TryGetAnonymousObjectCreationOperation(out var anonymousObjectCreationOperation))
                {
                    foreach (ISimpleAssignmentOperation initializer in anonymousObjectCreationOperation.Initializers)
                        AnalyzePropertyValue(initializer.Value, context.ReportDiagnostic);
                }
                else if (extendedPropertiesArgumentValue.TryGetDictionaryExtendedPropertyValueOperations(out var dictionaryExtendedPropertyValues))
                {
                    foreach (var extendedPropertyValue in dictionaryExtendedPropertyValues)
                    {
                        AnalyzePropertyValue(extendedPropertyValue!, context.ReportDiagnostic);
                    }
                }
            }

            private void AnalyzePropertyValue(IOperation propertyValue, Action<Diagnostic> reportDiagnostic)
            {
                if (propertyValue.Type is null || propertyValue.Type.IsValueType())
                {
                    return;
                }

                var publicProperties = propertyValue.Type.GetPublicProperties();
                GetRuntimeDecorateTargets(out var runtimeSafeToLogTargets, out var runtimeNotSafeToLogTargets);

                if (IsDecoratedWithSafeToLogAttribute(propertyValue.Type, runtimeSafeToLogTargets))
                {
                    if (publicProperties.Any(p => IsNotDecoratedWithNotSafeToLogAttribute(p, runtimeNotSafeToLogTargets)))
                    {
                        return;
                    }
                }
                else
                {
                    if (publicProperties.Any(p => IsDecoratedWithSafeToLogAttribute(p, runtimeSafeToLogTargets)))
                    {
                        return;
                    }
                }

                // "The '{0}' type does not have any properties marked as safe to log"
                var diagnostic = Diagnostic.Create(Rule, propertyValue.Syntax.GetLocation(), propertyValue.Type);
                reportDiagnostic(diagnostic);
            }

            private void GetRuntimeDecorateTargets(
                out IReadOnlyList<ISymbol> runtimeSafeToLogTargets,
                out IReadOnlyList<ISymbol> runtimeNotSafeToLogTargets)
            {
                var visitor = new SyntaxWalker(_safeToLogAttributeType, _notSafeToLogAttributeType, _compilation, _cancellationToken);
                runtimeSafeToLogTargets = visitor.SafeToLogTargets;
                runtimeNotSafeToLogTargets = visitor.NotSafeToLogTargets;
            }

            private bool IsDecoratedWithSafeToLogAttribute(ISymbol symbol, IReadOnlyList<ISymbol> safeToLogDecorateTargets) =>
                symbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _safeToLogAttributeType))
                || safeToLogDecorateTargets.Any(
                    target => SymbolEqualityComparer.Default.Equals(symbol, target));

            private bool IsNotDecoratedWithNotSafeToLogAttribute(ISymbol symbol, IReadOnlyList<ISymbol> notSafeToLogDecorateTargets) =>
                !symbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _notSafeToLogAttributeType))
                && !notSafeToLogDecorateTargets.Any(target =>
                    SymbolEqualityComparer.Default.Equals(symbol, target));

            private sealed class SyntaxWalker : CSharpSyntaxWalker
            {
                private readonly List<ISymbol> _safeToLogTargets = new List<ISymbol>();
                private readonly List<ISymbol> _notSafeToLogTargets = new List<ISymbol>();
                private readonly INamedTypeSymbol _safeToLogAttributeType;
                private readonly INamedTypeSymbol _notSafeToLogAttributeType;
                private readonly CancellationToken _cancellationToken;
                private readonly Compilation _compilation;

                // TODO: Pass the compilation in on construction
                // and call Visit(compilation) right away...well, the "root" one.
                // Make sure tests pass first.
                public SyntaxWalker(INamedTypeSymbol safeToLogAttributeType, INamedTypeSymbol notSafeToLogAttributeType, 
                    Compilation compilation, CancellationToken cancellationToken)
                {
                    _safeToLogAttributeType = safeToLogAttributeType;
                    _notSafeToLogAttributeType = notSafeToLogAttributeType;
                    _cancellationToken = cancellationToken;
                    _compilation = compilation;

                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        Visit(syntaxTree.GetRoot(_cancellationToken));
                    }
                }

                public IReadOnlyList<ISymbol> SafeToLogTargets => _safeToLogTargets;
                
                public IReadOnlyList<ISymbol> NotSafeToLogTargets => _notSafeToLogTargets;

                public override void VisitInvocationExpression(InvocationExpressionSyntax node)
                {
                    if (node.Expression is MemberAccessExpressionSyntax memberAccess
                        && memberAccess.Name is SimpleNameSyntax simpleName
                        && simpleName.Identifier.Text == "Decorate"
                        && _compilation!.GetSemanticModel(node.SyntaxTree) is SemanticModel semanticModel
                        && semanticModel.GetOperation(node, _cancellationToken) is IInvocationOperation invocation)
                    {
                        if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _safeToLogAttributeType))
                        {
                            AddTarget(invocation, _safeToLogTargets);
                        }
                        else if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, _notSafeToLogAttributeType))
                        {
                            AddTarget(invocation, _notSafeToLogTargets);
                        }
                    }

                    base.VisitInvocationExpression(node);
                }

                private static void AddTarget(IInvocationOperation invocation, IList<ISymbol> targets)
                {
                    if (invocation.TargetMethod.TypeArguments.Length > 0)
                    {
                        if (invocation.Arguments.Length == 0)
                        {
                            targets.Add(invocation.TargetMethod.TypeArguments[0]);
                        }
                        else if (invocation.Arguments[0].Value is IConversionOperation conversion
                            && conversion.Operand is IAnonymousFunctionOperation anonymousFunction
                            && anonymousFunction.Body is IBlockOperation block
                            && block.Operations.Length == 1
                            && block.Operations[0] is IReturnOperation returnOperation
                            && returnOperation.ReturnedValue is IConversionOperation conversion2
                            && conversion2.Operand is IPropertyReferenceOperation property)
                        {
                            targets.Add(property.Property);
                        }
                    }
                    else if (invocation.Arguments[0].Parameter!.Type.Name == "Type"
                        && invocation.Arguments[0].Value is ITypeOfOperation typeOfOperation)
                    {
                        targets.Add(typeOfOperation.TypeOperand);
                    }
                }
            }
        }
    }
}
