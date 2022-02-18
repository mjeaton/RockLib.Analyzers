using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    public static class AddInfoLogAttributeCodeFixProviderTests
    {
        [Fact]
        public static async Task CodeFix1()
        {
            await TestAssistants.VerifyCodeFixAsync<AddInfoLogAttributeAnalyzer, AddInfoLogAttributeCodeFixProvider>(
@"using Microsoft.AspNetCore.Mvc;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class [|TestController|] : ControllerBase
    {
    }
}
", 
@"using Microsoft.AspNetCore.Mvc;
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
").ConfigureAwait(false);
        }

        [Fact]
        public static async Task CodeFix2()
        {
            await TestAssistants.VerifyCodeFixAsync<AddInfoLogAttributeAnalyzer, AddInfoLogAttributeCodeFixProvider>(
@"using Microsoft.AspNetCore.Mvc;
using RockLib.Logging.AspNetCore;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [InfoLog]
        public string Get()
        {
            return ""Get"";
        }

        [HttpPut]
        public string [|Put|]()
        {
            return ""Put"";
        }

        [HttpPost]
        public string [|Post|]()
        {
            return ""Post"";
        }
    }
}
", 
@"using Microsoft.AspNetCore.Mvc;
using RockLib.Logging.AspNetCore;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [InfoLog]
        public string Get()
        {
            return ""Get"";
        }

        [HttpPut]
        [InfoLog]
        public string Put()
        {
            return ""Put"";
        }

        [HttpPost]
        [InfoLog]
        public string Post()
        {
            return ""Post"";
        }
    }
}
").ConfigureAwait(false);
        }
    }
}
