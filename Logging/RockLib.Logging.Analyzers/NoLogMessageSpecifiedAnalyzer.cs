using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RockLib.Analyzers.Common;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoLogMessageSpecifiedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "No message specified";
        private static readonly LocalizableString _messageFormat = "A message for the LogEntry cannot be null or empty";
        private static readonly LocalizableString _description = "Logs should have a message.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.NoMessageSpecified,
            _title,
            _messageFormat,
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: string.Format(HelpLinkUri.Format, DiagnosticIds.NoMessageSpecified));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var iloggerType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.ILogger");
            if (iloggerType == null)
                return;

            var stringType = context.Compilation.GetTypeByMetadataName("System.String");
            if (stringType == null)
                return;

            var analyzer = new OperationAnalyzer(iloggerType, stringType);
            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
        }

        private class OperationAnalyzer
        {
            private readonly INamedTypeSymbol _iloggerType;
            private readonly INamedTypeSymbol _stringType;

            public OperationAnalyzer(INamedTypeSymbol iloggerType, INamedTypeSymbol stringType)
            {
                _iloggerType = iloggerType;
                _stringType = stringType;
            }

            public void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;
                Location syntaxLocation = null;
                if (methodSymbol.MethodKind == MethodKind.Ordinary
                    && methodSymbol.Name == "Log"
                    && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _iloggerType))
                {
                    var logEntryArgument = invocationOperation.Arguments[0];
                    var logEntryCreation = logEntryArgument.GetLogEntryCreationOperation();

                    if (logEntryCreation == null
                       || IsMessageSet(logEntryCreation, logEntryArgument.Value))
                    {
                        return;
                    }

                    syntaxLocation = logEntryArgument.Syntax.GetLocation();
                }
                else if (methodSymbol.IsLoggingExtensionMethod())
                {
                    var arguments = invocationOperation.Arguments;
                    var messageArg = arguments.FirstOrDefault(argument => argument.Parameter.Name == "message");

                    if (messageArg.Value.ConstantValue.HasValue
                        && string.IsNullOrEmpty(messageArg.Value.ConstantValue.Value.ToString()))
                    {
                        syntaxLocation = messageArg.Syntax.GetLocation();
                    }
                    else
                    {
                        return;
                    }

                }

                if (syntaxLocation != null)
                {
                    var diagnostic = Diagnostic.Create(Rule, syntaxLocation);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            private bool IsMessageSet(IObjectCreationOperation logEntryCreation, IOperation logEntryArgumentValue)
            {
                if (logEntryCreation.Arguments.Length > 0)
                {
                    var levelArgument = logEntryCreation.Arguments.First(a => a.Parameter.Name == "message");
                    if (!levelArgument.IsImplicit
                        && levelArgument.Value.ConstantValue.HasValue
                        && !string.IsNullOrEmpty(levelArgument.Value.ConstantValue.Value.ToString()))
                        return true;
                }

                if (logEntryCreation.Initializer != null)
                {
                    foreach (var initializer in logEntryCreation.Initializer.Initializers)
                    {
                        if (initializer is ISimpleAssignmentOperation assignment
                            && assignment.Target is IPropertyReferenceOperation property
                            && SymbolEqualityComparer.Default.Equals(property.Type, _stringType)
                            && assignment.Value.ConstantValue.HasValue
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

            private class SimpleAssignmentWalker : OperationWalker
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
