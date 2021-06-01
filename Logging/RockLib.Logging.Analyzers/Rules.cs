using Microsoft.CodeAnalysis;

namespace RockLib.Logging.Analyzers
{
    public static class Rules
    {
        public const string HelpLinkFormat = "https://github.com/RockLib/RockLib.Analyzers/blob/master/Rules/{0}.md";

        public static DiagnosticDescriptor RockLib0000 { get; } = GetRule(
            nameof(RockLib0000),
            "Extended property is not marked as safe to log",
            "The value of a sanitized extended property should have a type with one or more properties decorated with the [SafeToLog] or else be decorated with the [SafeToLog] attribute itself.",
            DiagnosticCategory.Usage,
            DiagnosticSeverity.Warning);

        private static DiagnosticDescriptor GetRule(string id, string title, string messageFormat,
            DiagnosticCategory category, DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(id, title, messageFormat, category.ToString(), severity,
                isEnabledByDefault: true, helpLinkUri: string.Format(HelpLinkFormat, id));
    }
}
