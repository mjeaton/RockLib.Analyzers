using Microsoft.CodeAnalysis;

namespace RockLib.Analyzers.Common
{
    internal static class CommonExtensions
    {
        public static IOperation GetRootOperation(this IOperation operation)
        {
            while (operation.Parent is not null)
            {
                operation = operation.Parent;
            }

            return operation;
        }
    }
}
