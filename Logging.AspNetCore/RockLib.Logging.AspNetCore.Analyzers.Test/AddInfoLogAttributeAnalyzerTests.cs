using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using RockLibVerifier = RockLib.Logging.AspNetCore.Analyzers.Test.CSharpAnalyzerVerifier<
    RockLib.Logging.AspNetCore.Analyzers.AddInfoLogAttributeAnalyzer>;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    [TestClass]
    public class AddInfoLogAttributeAnalyzerTests
    {
        [TestMethod("Diagnostics are reported on type and action methods when class name ends in 'Controller'")]
        public async Task DiagnosticReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;

public class [|TestController|]
{
    // Action methods:

    public string [|Get|]() => ""Get"";

    // Non-action methods:

    public TestController()
    {
    }

    public static string Foo(string foo) => foo;

    private string Bar(string bar) => bar;

    [NonAction]
    public string Baz(string baz) => baz;

    public string Qux<T>(string qux) => qux;

    public override string ToString() => ""TestController"";
}");
        }

        [TestMethod("Diagnostics are reported on type and action methods when class is decorated with [Controller] attribute")]
        public async Task DiagnosticReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;

[Controller]
public class [|Test|]
{
    // Action methods:

    public string [|Get|]() => ""Get"";

    // Non-action methods:

    public Test()
    {
    }

    public static string Foo(string foo) => foo;

    private string Bar(string bar) => bar;

    [NonAction]
    public string Baz(string baz) => baz;

    public string Qux<T>(string qux) => qux;

    public override string ToString() => ""TestController"";
}");
        }

        [TestMethod("No diagnostics are reported when controller is decorated with [InfoLog] attribute")]
        public async Task NoDiagnosticsReported1()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging.AspNetCore;

[InfoLog]
public class TestController
{
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when action methods are decorated with [InfoLog] attribute")]
        public async Task NoDiagnosticsReported2()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using RockLib.Logging.AspNetCore;

public class TestController
{
    [InfoLog]
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when type is struct")]
        public async Task NoDiagnosticsReported3()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
public struct TestController
{
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when class is abstract")]
        public async Task NoDiagnosticsReported4()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
public abstract class TestController
{
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when class is not public")]
        public async Task NoDiagnosticsReported5()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
internal class TestController
{
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when class is generic")]
        public async Task NoDiagnosticsReported6()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
public class TestController<T>
{
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when class is decorated with [NonController] attribute")]
        public async Task NoDiagnosticsReported7()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Mvc;

[NonController]
public class TestController
{
    public string Get() => ""Get"";
}");
        }

        [TestMethod("No diagnostics are reported when class is not decorated with [Controller] attribute and name does not end with 'Controller'")]
        public async Task NoDiagnosticsReported8()
        {
            await RockLibVerifier.VerifyAnalyzerAsync(@"
public class Test
{
    public string Get() => ""Get"";
}");
        }
    }
}
