using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RockLib.Logging.Analyzers
{
    internal class SimpleAssignmentWalker : OperationWalker
    {
        private readonly ILocalReferenceOperation _logEntryReference;

        public SimpleAssignmentWalker(ILocalReferenceOperation logEntryReference)
        {
            _logEntryReference = logEntryReference;
        }

        public bool IsExceptionSet { get; private set; }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            if (operation.Target is IPropertyReferenceOperation property
                && property.Property.Name == "Exception"
                && property.Instance is ILocalReferenceOperation localReference
                && SymbolEqualityComparer.Default.Equals(localReference.Local, _logEntryReference.Local))
            {
                var a = property.Property as IMemberReferenceOperation;
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
