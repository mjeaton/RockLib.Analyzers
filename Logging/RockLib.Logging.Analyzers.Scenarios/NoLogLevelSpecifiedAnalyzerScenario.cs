namespace RockLib.Logging.Analyzers.Scenarios
{
    public static class NoLogLevelSpecifiedAnalyzerScenario
    {
        public static void Do(ILogger logger)
        {
            logger.Log(new LogEntry("Hello, world!"));
        }
    }
}