using Microsoft.CodeAnalysis;

namespace RockLib.Logging.Analyzers
{
    public static class Rules
    {
        public const string HelpLinkFormat = "https://github.com/RockLib/RockLib.Analyzers/blob/master/Rules/{0}.md";

        private static DiagnosticDescriptor GetRule(string id, string title, string messageFormat,
            DiagnosticCategory category, DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(id, title, messageFormat, category.ToString(), severity,
                isEnabledByDefault: true, helpLinkUri: string.Format(HelpLinkFormat, id));
    }
}
