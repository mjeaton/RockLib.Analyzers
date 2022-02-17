using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoLogLevelSpecifiedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "No log level specified";
        private static readonly LocalizableString _messageFormat = "The Level of the LogEntry is not specified";
        private static readonly LocalizableString _description = "Logs should specify their level.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.NoLogLevelSpecified,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.NoLogLevelSpecified));

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
            var iloggerType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.ILogger");
            if (iloggerType is null) { return; }

            var logLevelType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LogLevel");
            if (logLevelType is null) { return; }

            var analyzer = new InvocationOperationAnalyzer(iloggerType, logLevelType);

            context.RegisterOperationAction(analyzer.Analyze, OperationKind.Invocation);
        }

        private sealed class InvocationOperationAnalyzer
        {
            private readonly INamedTypeSymbol _iloggerType;
            private readonly INamedTypeSymbol _logLevelType;

            public InvocationOperationAnalyzer(INamedTypeSymbol iloggerType, INamedTypeSymbol logLevelType)
            {
                _iloggerType = iloggerType;
                _logLevelType = logLevelType;
            }

            public void Analyze(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind != MethodKind.Ordinary
                    || methodSymbol.Name != "Log"
                    || !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _iloggerType))
                {
                    return;
                }

                var logEntryArgument = invocationOperation.Arguments[0];
                var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation();

                if (logEntryCreation is null
                    || IsLevelSet(logEntryCreation, logEntryArgument.Value))
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(Rule, logEntryArgument.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }

            private bool IsLevelSet(IObjectCreationOperation logEntryCreation, IOperation logEntryArgumentValue)
            {
                if (logEntryCreation.Arguments.Length > 0)
                {
                    var levelArgument = logEntryCreation.Arguments.First(a => a.Parameter!.Name == "level");
                    if (!levelArgument.IsImplicit)
                    {
                        return true;
                    }
                }

                if (logEntryCreation.Initializer is not null)
                {
                    foreach (var initializer in logEntryCreation.Initializer.Initializers)
                    {
                        if (initializer is ISimpleAssignmentOperation assignment
                            && assignment.Target is IPropertyReferenceOperation property
                            && SymbolEqualityComparer.Default.Equals(property.Type, _logLevelType))
                        {
                            return true;
                        }
                    }
                }

                if (logEntryArgumentValue is ILocalReferenceOperation logEntryReference)
                {
                    var visitor = new SimpleAssignmentWalker(_logLevelType, logEntryReference);
                    visitor.Visit(logEntryCreation.GetRootOperation());
                    return visitor.IsLevelSet;
                }

                return false;
            }

            private sealed class SimpleAssignmentWalker : OperationWalker
            {
                private readonly INamedTypeSymbol _logLevelType;
                private readonly ILocalReferenceOperation _logEntryReference;

                public SimpleAssignmentWalker(INamedTypeSymbol logLevelType, ILocalReferenceOperation logEntryReference)
                {
                    _logLevelType = logLevelType;
                    _logEntryReference = logEntryReference;
                }

                public bool IsLevelSet { get; private set; }

                public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
                {
                    if (operation.Target is IPropertyReferenceOperation property
                        && SymbolEqualityComparer.Default.Equals(property.Type, _logLevelType)
                        && property.Instance is ILocalReferenceOperation localReference
                        && SymbolEqualityComparer.Default.Equals(localReference.Local, _logEntryReference.Local))
                    {
                        IsLevelSet = true;
                    }

                    base.VisitSimpleAssignment(operation);
                }
            }
        }
    }
}
