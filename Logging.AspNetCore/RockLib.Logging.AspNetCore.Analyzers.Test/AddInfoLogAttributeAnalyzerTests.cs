using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.AspNetCore.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.AspNetCore.Analyzers.AddInfoLogAttributeAnalyzer>;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    [TestClass]
    public class AddInfoLogAttributeAnalyzerTests
    {
        [TestMethod("Diagnostics are reported when type inherits from ControllerBase and does not have InfoLog attribute")]
        public async Task TestTestMethod()
        {
            var test = @"
using Microsoft.AspNetCore.Mvc;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class [|TestController|] : ControllerBase
    {
    }
}
";
            await RockLibVerifier.VerifyAnalyzerAsync(test);
        }
    }
}
