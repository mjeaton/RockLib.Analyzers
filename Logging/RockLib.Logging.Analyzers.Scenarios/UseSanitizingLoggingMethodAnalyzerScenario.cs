namespace RockLib.Logging.Analyzers.Scenarios
{
    public static class UseSanitizingLoggingMethodAnalyzerScenario
    {
        public static void Do(LogEntry entry, Client client)
        {
            entry.ExtendedProperties["a"] = client;
        }

        public sealed class Client { }
    }
}