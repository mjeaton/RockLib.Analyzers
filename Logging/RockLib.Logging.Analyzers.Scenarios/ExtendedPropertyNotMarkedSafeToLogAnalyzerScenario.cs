using RockLib.Logging.SafeLogging;

namespace RockLib.Logging.Analyzers.Scenarios
{
    public static class ExtendedPropertyNotMarkedSafeToLogAnalyzerScenario
    {
        public static void Do(ILogger logger, Client client)
        {
            logger.InfoSanitized("Example message", new { Client = client });
        }

        public sealed class Client
        {
            public string? Name { get; }
        }
    }
}