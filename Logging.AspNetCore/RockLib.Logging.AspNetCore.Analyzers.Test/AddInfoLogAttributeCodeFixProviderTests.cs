using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.AspNetCore.Analyzers.Test.CSharpCodeFixVerifier<
    RockLib.Logging.AspNetCore.Analyzers.AddInfoLogAttributeAnalyzer,
    RockLib.Logging.AspNetCore.Analyzers.AddInfoLogAttributeCodeFixProvider>;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    [TestClass]
    public class AddInfoLogAttributeCodeFixProviderTests
    {
        [TestMethod]
        public async Task CodeFix1()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Mvc;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class [|TestController|] : ControllerBase
    {
    }
}
", @"
using Microsoft.AspNetCore.Mvc;
using RockLib.Logging.AspNetCore;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    [InfoLog]
    public class TestController : ControllerBase
    {
    }
}
");
        }

        [TestMethod]
        public async Task CodeFix2()
        {
            await RockLibVerifier.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Mvc;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class [|TestController|] : ControllerBase
    {
    }
}
", @"
using Microsoft.AspNetCore.Mvc;
using RockLib.Logging.AspNetCore;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    [InfoLog]
    public class TestController : ControllerBase
    {
    }
}
");
        }
    }
}
