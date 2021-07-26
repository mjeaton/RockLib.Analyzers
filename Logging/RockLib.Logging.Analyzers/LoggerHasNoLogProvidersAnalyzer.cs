using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LoggerHasNoLogProvidersAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Logger has no log providers";
        private static readonly LocalizableString _messageFormat = "";
        private static readonly LocalizableString _description = "";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.LoggerHasNoLogProviders,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.LoggerHasNoLogProviders));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var serviceCollectionType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.DependencyInjection.ServiceCollectionExtensions");
            if (serviceCollectionType == null)
                return;

            var iloggerBuilderType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.DependencyInjection.ILoggerBuilder");
            if (iloggerBuilderType == null)
                return;

            var iloggerOptionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.DependencyInjection.ILoggerOptions");
            if (iloggerOptionsType == null)
                return;

            var analyzer = new InvocationOperationAnalyzer(serviceCollectionType, iloggerBuilderType, iloggerOptionsType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _serviceCollectionType;
            private readonly INamedTypeSymbol _iloggerBuilderType;
            private readonly INamedTypeSymbol _iloggerOptionsType;

            public InvocationOperationAnalyzer(INamedTypeSymbol serviceCollectionType,
                INamedTypeSymbol iloggerBuilderType, INamedTypeSymbol iloggerOptionsType)
            {
                _serviceCollectionType = serviceCollectionType;
                _iloggerBuilderType = iloggerBuilderType;
                _iloggerOptionsType = iloggerOptionsType;
            }

            public void Analyze(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary
                    || !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _serviceCollectionType))
                {
                    return;
                }

                var loggerBuilderInvocations = FindLoggerBuilderInvocations.For(invocationOperation, _iloggerBuilderType);

                if (loggerBuilderInvocations.Any(invocation => invocation.TargetMethod.Name.Contains("LogProvider")))
                    return;
                new /*wtf*/ LoggerHasNoLogProvidersAnalyzer ( ) ;
                if (loggerBuilderInvocations.Any(invocation => invocation.TargetMethod.Name.Contains("ContextProvider"))
                    || IsLevelOrIsDisabledSet.For(invocationOperation, _iloggerOptionsType))
                {
                    var diagnostic = Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                // The AddLogger() call has been determined to be "empty". Need to look in appsettings.json (and maybe
                // launch.json (for VS - need to determine files for VSCode and Rider) for environment variables).

                if (GetAppsettingsJson(context) is string appsettingsJson)
                {
                    // Ok, now what?
                }
            }

            private static string GetAppsettingsJson(OperationAnalysisContext context)
            {
                foreach (AdditionalText text in context.Options.AdditionalFiles)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    string fileName = Path.GetFileName(text.Path);
                    if (fileName.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase))
                    {
                        return text.GetText(context.CancellationToken).ToString();
                    }
                }

                return null;
            }

            private class FindLoggerBuilderInvocations : OperationWalker
            {
                private readonly List<IInvocationOperation> _invocations = new List<IInvocationOperation>();
                private readonly ILocalSymbol _localSymbol;
                private readonly ITypeSymbol _iloggerBuilderType;

                private FindLoggerBuilderInvocations(ILocalSymbol localSymbol, ITypeSymbol iloggerBuilderType)
                {
                    _localSymbol = localSymbol;
                    _iloggerBuilderType = iloggerBuilderType;
                }

                public static IReadOnlyList<IInvocationOperation> For(IInvocationOperation addLoggerInvocationOperation, ITypeSymbol iloggerBuilderType)
                {
                    var loggerBuilderInvocations = new List<IInvocationOperation>();
                    ILocalSymbol localSymbol = null;

                    IOperation operation = addLoggerInvocationOperation.Parent;
                    while (true)
                    {
                        if (operation is IInvocationOperation invocationOperation)
                        {
                            if (invocationOperation.TargetMethod.IsStatic
                                && invocationOperation.TargetMethod.Parameters.Length > 0
                                && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.ReturnType, iloggerBuilderType)
                                && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.Parameters[0].Type, iloggerBuilderType))
                            {
                                loggerBuilderInvocations.Add(invocationOperation);
                            }
                        }
                        else if (operation is IVariableDeclaratorOperation variableDeclaratorOperation)
                        {
                            localSymbol = variableDeclaratorOperation.Symbol;
                        }

                        if (operation.Parent == null)
                            break;

                        operation = operation.Parent;
                    }

                    if (localSymbol != null)
                    {
                        var visitor = new FindLoggerBuilderInvocations(localSymbol, iloggerBuilderType);
                        visitor.Visit(operation);
                        loggerBuilderInvocations.AddRange(visitor._invocations);
                    }

                    return loggerBuilderInvocations;
                }

                public override void VisitInvocation(IInvocationOperation operation)
                {
                    if (operation.TargetMethod.IsStatic
                        && operation.TargetMethod.Parameters.Length > 0
                        && SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ReturnType, _iloggerBuilderType)
                        && SymbolEqualityComparer.Default.Equals(operation.TargetMethod.Parameters[0].Type, _iloggerBuilderType)
                        && operation.Arguments[0].Value is ILocalReferenceOperation localReferenceOperation
                        && SymbolEqualityComparer.Default.Equals(localReferenceOperation.Local, _localSymbol))
                    {
                        _invocations.Add(operation);
                    }

                    base.VisitInvocation(operation);
                }
            }

            private class IsLevelOrIsDisabledSet : OperationWalker
            {
                private readonly IParameterSymbol _optionsParameter;
                private readonly INamedTypeSymbol _iloggerOptionsType;
                private bool _isLevelOrIsDisabledSet;

                private IsLevelOrIsDisabledSet(IParameterSymbol optionsParameter, INamedTypeSymbol iloggerOptionsType)
                {
                    _optionsParameter = optionsParameter;
                    _iloggerOptionsType = iloggerOptionsType;
                }

                public static bool For(IInvocationOperation addLoggerInvocationOperation, INamedTypeSymbol iloggerOptionsType)
                {
                    var configureOptionsArgument = addLoggerInvocationOperation.Arguments.FirstOrDefault(a => a.Parameter.Name == "configureOptions" && !a.IsImplicit);

                    if (configureOptionsArgument?.Value is IDelegateCreationOperation delegateCreationOperation
                        && delegateCreationOperation.Target is IAnonymousFunctionOperation anonymousFunctionOperation)
                    {
                        var visitor = new IsLevelOrIsDisabledSet(anonymousFunctionOperation.Symbol.Parameters[0], iloggerOptionsType);
                        visitor.Visit(anonymousFunctionOperation.Body);
                        return visitor._isLevelOrIsDisabledSet;
                    }

                    return false;
                }

                public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
                {
                    if (operation.Target is IPropertyReferenceOperation propertyReferenceOperation
                        && (propertyReferenceOperation.Property.Name == "Level"
                            || propertyReferenceOperation.Property.Name == "IsDisabled")
                        && !(operation.Value is IConversionOperation conversion
                            && conversion.Operand is ILiteralOperation literal
                            && literal.ConstantValue.HasValue 
                            && literal.ConstantValue.Value == null))
                    {
                        _isLevelOrIsDisabledSet = true;
                    }

                    base.VisitSimpleAssignment(operation);
                }
            }
        }
    }
}
