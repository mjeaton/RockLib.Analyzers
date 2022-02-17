using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RockLib.Logging.Analyzers
{
    internal sealed class SimpleAssignmentWalker : OperationWalker
    {
        private readonly ILocalReferenceOperation _logEntryReference;

        public SimpleAssignmentWalker(ILocalReferenceOperation logEntryReference, IOperation root)
        {
            _logEntryReference = logEntryReference;
            Visit(root);
        }

        public bool IsExceptionSet { get; private set; }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            if (operation.Target is IPropertyReferenceOperation property
                && property.Property.Name == "Exception"
                && property.Instance is ILocalReferenceOperation localReference
                && SymbolEqualityComparer.Default.Equals(localReference.Local, _logEntryReference.Local))
            {
                IsExceptionSet = true;
            }
            else if (operation.Target is IPropertyReferenceOperation propertyRef
                && propertyRef.Property.Name == "Exception"
                && operation.Value is IConversionOperation conversionOperation
                && conversionOperation.Operand is ILocalReferenceOperation localRef
                && SymbolEqualityComparer.Default.Equals(localRef.Local, _logEntryReference.Local))
            {
                IsExceptionSet = true;
            }

            base.VisitSimpleAssignment(operation);
        }
    }
}
