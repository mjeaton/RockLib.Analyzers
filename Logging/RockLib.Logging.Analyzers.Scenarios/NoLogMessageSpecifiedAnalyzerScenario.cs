namespace RockLib.Logging.Analyzers.Scenarios
{
    public static class NoLogMessageSpecifiedAnalyzerScenario
    {
        public static void Do(ILogger logger)
        {
            logger.Log(new LogEntry() { Level = LogLevel.Debug });
        }
    }
}