using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Linq;

namespace RockLib.Logging.Microsoft.Extensions.Analyzers
{
    internal static class CommonExtensions
    {
        public static string GetLoggerName(this ImmutableArray<IArgumentOperation> arguments)
        {
            if (arguments.FirstOrDefault(IsLoggerNameArgument) is IArgumentOperation argument
                && argument.Value is ILiteralOperation literal
                && literal.ConstantValue.HasValue)
            {
                return (string)literal.ConstantValue.Value!;
            }

            return "";

            static bool IsLoggerNameArgument(IArgumentOperation arg) =>
                arg.Parameter!.Name == "loggerName" || arg.Parameter.Name == "rockLibLoggerName";
        }
    }
}
