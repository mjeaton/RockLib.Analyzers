namespace RockLib.Logging.Analyzers.Scenarios
{
    public static class UnexpectedExtendedPropertiesObjectAnalyzerScenario
    {
        public static void Do(ILogger logger)
        {
            var data = new Data();
            logger.Info("Some message", data);
        }

        public sealed class Data { }
    }
}