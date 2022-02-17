using RockLib.Logging.SafeLogging;
using System;

namespace RockLib.Logging.Analyzers.Scenarios
{
    internal static class CaughtExceptionShouldBeLoggedAnalyzerScenario
    {
        public static void Use(ILogger logger)
        {
            try
            {
            }
            catch (Exception ex)
            {
                logger.Debug("A debug log without exception");
                logger.DebugSanitized("A debug log without exception", new { foo = 123 });
            }
        }
    }
}
