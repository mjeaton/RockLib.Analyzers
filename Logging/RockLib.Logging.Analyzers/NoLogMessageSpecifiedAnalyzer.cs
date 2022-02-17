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
    public sealed class NoLogMessageSpecifiedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "No message specified";
        private static readonly LocalizableString _messageFormat = "The message cannot be null or empty";
        private static readonly LocalizableString _description = "Logs should have a message.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.NoMessageSpecified,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(CultureInfo.InvariantCulture, HelpLinkUri.Format, DiagnosticIds.NoMessageSpecified));

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

            var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
            if (loggingExtensionsType is null) { return; }

            var safeLoggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.SafeLogging.SafeLoggingExtensions");
            if (safeLoggingExtensionsType is null) { return; }

            var stringType = context.Compilation.GetTypeByMetadataName("System.String");
            if (stringType is null) { return; }

            var analyzer = new OperationAnalyzer(iloggerType, loggingExtensionsType, safeLoggingExtensionsType, stringType);
            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
        }

        private sealed class OperationAnalyzer
        {
            private readonly INamedTypeSymbol _iloggerType;
            private readonly INamedTypeSymbol _loggingExtensionType;
            private readonly INamedTypeSymbol _safeLoggingExtensionType;
            private readonly INamedTypeSymbol _stringType;

            public OperationAnalyzer(INamedTypeSymbol iloggerType, INamedTypeSymbol loggingExtensionsType, INamedTypeSymbol safeLoggingExtensionType, INamedTypeSymbol stringType)
            {
                _iloggerType = iloggerType;
                _loggingExtensionType = loggingExtensionsType;
                _safeLoggingExtensionType = safeLoggingExtensionType;
                _stringType = stringType;
            }

            public void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;
                Location? syntaxLocation = null;
                if (methodSymbol.MethodKind == MethodKind.Ordinary
                    && methodSymbol.Name == "Log"
                    && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _iloggerType))
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation();

                    if (logEntryCreation is null
                       || IsMessageSet(logEntryCreation, logEntryArgument.Value))
                    {
                        return;
                    }

                    syntaxLocation = logEntryArgument.Syntax.GetLocation();
                }
                else if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggingExtensionType)
                    || SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _safeLoggingExtensionType))
                {
                    var arguments = invocationOperation.Arguments;
                    var messageArg = arguments.First(argument => argument.Parameter!.Name == "message");

                    if (messageArg.Value.ConstantValue.HasValue
                        && (messageArg.Value.ConstantValue.Value is null
                        || string.IsNullOrEmpty(messageArg.Value.ConstantValue.Value.ToString())))
                    {
                        syntaxLocation = messageArg.Syntax.GetLocation();
                    }
                    else
                    {
                        return;
                    }

                }

                if (syntaxLocation is not null)
                {
                    var diagnostic = Diagnostic.Create(Rule, syntaxLocation);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private bool IsMessageSet(IObjectCreationOperation logEntryCreation, IOperation logEntryArgumentValue)
            {
                if (logEntryCreation.Arguments.Length > 0)
                {
                    var levelArgument = logEntryCreation.Arguments.First(a => a.Parameter!.Name == "message");
                    if (!levelArgument.IsImplicit
                        && levelArgument.Value.ConstantValue.HasValue
                        && levelArgument.Value.ConstantValue.Value is not null
                        && !string.IsNullOrEmpty(levelArgument.Value.ConstantValue.Value.ToString()))
                        return true;
                }

                if (logEntryCreation.Initializer is not null)
                {
                    foreach (var initializer in logEntryCreation.Initializer.Initializers)
                    {
                        if (initializer is ISimpleAssignmentOperation assignment
                            && assignment.Target is IPropertyReferenceOperation property
                            && SymbolEqualityComparer.Default.Equals(property.Type, _stringType)
                            && assignment.Value.ConstantValue.HasValue
                            && assignment.Value.ConstantValue.Value is not null
                            && !string.IsNullOrEmpty(assignment.Value.ConstantValue.Value.ToString()))
                        {
                            return true;
                        }
                    }
                }

                if (logEntryArgumentValue is ILocalReferenceOperation logEntryReference)
                {
                    var visitor = new SimpleAssignmentWalker(_stringType, logEntryReference);
                    visitor.Visit(logEntryCreation.GetRootOperation());
                    return visitor.IsMessageSet;
                }

                return false;
            }

            private sealed class SimpleAssignmentWalker : OperationWalker
            {
                private readonly INamedTypeSymbol _stringType;
                private readonly ILocalReferenceOperation _logEntryReference;

                public SimpleAssignmentWalker(INamedTypeSymbol stringType, ILocalReferenceOperation logEntryReference)
                {
                    _stringType = stringType;
                    _logEntryReference = logEntryReference;
                }

                public bool IsMessageSet { get; private set; }

                public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
                {
                    if (operation.Target is IPropertyReferenceOperation property
                        && SymbolEqualityComparer.Default.Equals(property.Type, _stringType)
                        && property.Parent is ISimpleAssignmentOperation parentProperty
                        && parentProperty.Value.ConstantValue.HasValue
                        && parentProperty.Value.ConstantValue.Value is not null
                        && !string.IsNullOrEmpty(parentProperty.Value.ConstantValue.Value.ToString())
                        && property.Instance is ILocalReferenceOperation local
                        && SymbolEqualityComparer.Default.Equals(local.Local, _logEntryReference.Local))
                    {
                        IsMessageSet = true;
                    }

                    base.VisitSimpleAssignment(operation);
                }
            }
        }
    }
}
