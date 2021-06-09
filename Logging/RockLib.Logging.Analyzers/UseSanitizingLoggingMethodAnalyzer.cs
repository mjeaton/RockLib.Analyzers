﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseSanitizingLoggingMethodAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString _title = "Use sanitizing logging method";
        private static readonly LocalizableString _messageFormat = "Use sanitizing logging method";
        private static readonly LocalizableString _description = "Use sanitizing logging method.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.UseSanitizingLoggingMethod,
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
            var logEntryType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LogEntry");
            if (logEntryType == null)
                return;

            var loggingExtensionsType = context.Compilation.GetTypeByMetadataName("RockLib.Logging.LoggingExtensions");
            if (loggingExtensionsType == null)
                return;

            var analyzer = new OperationAnalyzer(logEntryType, loggingExtensionsType);

            context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(analyzer.AnalyzeAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(analyzer.AnalyzeObjectCreation, OperationKind.ObjectCreation);
        }

        private class OperationAnalyzer
        {
            private readonly INamedTypeSymbol _logEntryType;
            private readonly INamedTypeSymbol _loggingExtensionsType;

            public OperationAnalyzer(INamedTypeSymbol logEntryType, INamedTypeSymbol loggingExtensionsType)
            {
                _logEntryType = logEntryType;
                _loggingExtensionsType = loggingExtensionsType;
            }

            public void AnalyzeInvocation(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;
                var methodSymbol = invocationOperation.TargetMethod;

                if (methodSymbol.MethodKind == MethodKind.Ordinary
                    && SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _loggingExtensionsType)
                    || (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, _logEntryType)
                        && methodSymbol.Name == "SetExtendedProperties"))
                {
                    AnalyzeExtendedPropertiesArgument(invocationOperation.Arguments, context.ReportDiagnostic, invocationOperation.Syntax);
                }
                else if ((methodSymbol.Name == "Add" || methodSymbol.Name == "TryAdd")
                    && invocationOperation.Instance is IPropertyReferenceOperation property
                    && property.Member.Name == "ExtendedProperties"
                    && SymbolEqualityComparer.Default.Equals(property.Instance.Type, _logEntryType)
                    && invocationOperation.Arguments[1].Value is IConversionOperation conversion
                    && !conversion.Operand.Type.IsValueType())
                {
                    var diagnostic = Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            public void AnalyzeAssignment(OperationAnalysisContext context)
            {
                var assignmentOperation = (ISimpleAssignmentOperation)context.Operation;

                if (assignmentOperation.Target is IPropertyReferenceOperation indexerOperation
                    && indexerOperation.Arguments.Length == 1
                    && indexerOperation.Arguments[0].Value.Type.SpecialType == SpecialType.System_String
                    && indexerOperation.Instance is IPropertyReferenceOperation extendedPropertiesOperation
                    && extendedPropertiesOperation.Arguments.Length == 0
                    && extendedPropertiesOperation.Property.Name == "ExtendedProperties"
                    && SymbolEqualityComparer.Default.Equals(extendedPropertiesOperation.Property.ContainingType, _logEntryType)
                    && assignmentOperation.Value is IConversionOperation conversion
                    && !conversion.Operand.Type.IsValueType())
                {
                    var diagnostic = Diagnostic.Create(Rule, assignmentOperation.Syntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            public void AnalyzeObjectCreation(OperationAnalysisContext context)
            {
                var objectCreationOperation = (IObjectCreationOperation)context.Operation;
                if (SymbolEqualityComparer.Default.Equals(objectCreationOperation.Type, _logEntryType))
                {
                    AnalyzeExtendedPropertiesArgument(objectCreationOperation.Arguments, context.ReportDiagnostic, objectCreationOperation.Syntax);
                }
            }

            private void AnalyzeExtendedPropertiesArgument(IEnumerable<IArgumentOperation> arguments, Action<Diagnostic> reportDiagnostic, SyntaxNode reportingNode)
            {
                var extendedPropertiesArgument = arguments.FirstOrDefault(argument => argument.Parameter.Name == "extendedProperties");

                if (extendedPropertiesArgument == null
                        || !(extendedPropertiesArgument.Value is IConversionOperation convertToObjectType)
                        || convertToObjectType.Type.SpecialType != SpecialType.System_Object)
                    return;

                var extendedPropertiesArgumentValue = convertToObjectType.Operand;

                if ((extendedPropertiesArgumentValue.TryGetAnonymousObjectCreationOperation(out var anonymousObjectCreationOperation)
                        && anonymousObjectCreationOperation.Initializers.Any(initializer => !((ISimpleAssignmentOperation)initializer).Value.Type.IsValueType()))
                    || (extendedPropertiesArgumentValue.TryGetDictionaryExtendedPropertyValueOperations(out var dictionaryExtendedPropertyValues)
                        && dictionaryExtendedPropertyValues.Any(value => !value.Type.IsValueType())))
                {
                    var diagnostic = Diagnostic.Create(Rule, reportingNode.GetLocation());
                    reportDiagnostic(diagnostic);
                }
            }
        }
    }
}
