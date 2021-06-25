using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.AspNetCore.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.AspNetCore.Analyzers.AddInfoLogAttributeAnalyzer>;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    [TestClass]
    public class AddInfoLogAttributeAnalyzerTests
    {
        [TestMethod("Diagnostics are reported when type inherits from ControllerBase and type does not have InfoLog attribute")]
        public async Task DiagnosticReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class [|TestController|] : ControllerBase
    {
    }
}
");
        }

        [TestMethod("Diagnostics are reported when type inherits from ControllerBase and type and methods do not have InfoLog attributes")]
        public async Task DiagnosticReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    public class [|TestController|] : ControllerBase
    {
        [HttpGet]
        public string [|Get|]()
        {
            return ""Get"";
        }
    }
}
");
        }

        [TestMethod("Diagnostics are reported when type inherits from ControllerBase and some methods do not have InfoLog attributes")]
        public async Task DiagnosticReported3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;
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
");
        }

        [TestMethod("Diagnostics are not reported when type inherits from ControllerBase and type has InfoLog attributes")]
        public async Task DiagnosticNotReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;
using RockLib.Logging.AspNetCore;

namespace UnitTestingNamespace
{
    [ApiController]
    [Route(""[controller]"")]
    [InfoLog]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return ""Get"";
        }

        [HttpPut]
        public string Put()
        {
            return ""Put"";
        }

        [HttpPost]
        public string Post()
        {
            return ""Post"";
        }
    }
}
");
        }

        [TestMethod("Diagnostics are not reported when type inherits from ControllerBase and all methods have InfoLog attributes")]
        public async Task DiagnosticNotReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;
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
");
        }
    }
}
