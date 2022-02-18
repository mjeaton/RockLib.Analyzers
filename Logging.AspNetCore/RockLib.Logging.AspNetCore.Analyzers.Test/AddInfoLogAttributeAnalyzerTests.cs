using System.Threading.Tasks;
using Xunit;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    public static class AddInfoLogAttributeAnalyzerTests
    {
        private const string _aspNetCoreStub = @"

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using AspNetCore.Mvc;

    public static class MvcServiceCollectionExtensions
    {
        public static IMvcBuilder AddControllers(this IServiceCollection services, Action<MvcOptions> configure)
        {
            return null;
        }
    }
}

namespace Microsoft.AspNetCore.Mvc
{
    using Filters;

    public class MvcOptions
    {
        public FilterCollection Filters { get; } = new FilterCollection();
    }
}

namespace Microsoft.AspNetCore.Mvc.Filters
{
    using System;
    using System.Collections.ObjectModel;

    public class FilterCollection : Collection<IFilterMetadata>
    {
        public IFilterMetadata Add(Type filterType) => null;
        public IFilterMetadata Add(Type filterType, int order) => null;
        public IFilterMetadata AddService(Type filterType) => null;
        public IFilterMetadata AddService(Type filterType, int order) => null;
        public IFilterMetadata AddService<TFilterType>() where TFilterType : IFilterMetadata => null;
        public IFilterMetadata AddService<TFilterType>(int order) where TFilterType : IFilterMetadata => null;
        public IFilterMetadata Add<TFilterType>() where TFilterType : IFilterMetadata => null;
        public IFilterMetadata Add<TFilterType>(int order) where TFilterType : IFilterMetadata => null;
    }
}";

        [Fact]
        public static async Task AnalyzeWhenClassNameEndsWithController()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"using Microsoft.AspNetCore.Mvc;

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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenMemberHasControllerAttribute()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"using Microsoft.AspNetCore.Mvc;

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
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenControllerHasInfoLogAttribute()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"using RockLib.Logging.AspNetCore;

[InfoLog]
public class TestController
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenMembersHaveInfoLogAttribute()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"using RockLib.Logging.AspNetCore;

public class TestController
{
    [InfoLog]
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenTypeIsStruct()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"public struct TestController
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenClassIsAbstract()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"public abstract class TestController
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenClassIsNotPublic()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"internal class TestController
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenClassIsGeneric()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"public class TestController<T>
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenClassHasNonControllerAttribute()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(
@"using Microsoft.AspNetCore.Mvc;

[NonController]
public class TestController
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenClassHasControllerAttributeAndNameDoesNotEndWithController()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(@"
public class Test
{
    public string Get() => ""Get"";
}").ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenInfoLogAttributeIsAddedToFiltersUsingGenericAddMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging.AspNetCore;

public class TestController
{
    public string Get() => ""Get"";
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<InfoLogAttribute>();
        });
    }
}" + _aspNetCoreStub).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenInfoLogAttributeIsAddedToFiltersUsingNonGenericAddMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging.AspNetCore;

public class TestController
{
    public string Get() => ""Get"";
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(typeof(InfoLogAttribute));
        });
    }
}" + _aspNetCoreStub).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenInfoLogAttributeIsAddedToFiltersUsingGenericAddServiceMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging.AspNetCore;

public class TestController
{
    public string Get() => ""Get"";
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.AddService<InfoLogAttribute>();
        });
    }
}" + _aspNetCoreStub).ConfigureAwait(false);
        }

        [Fact]
        public static async Task AnalyzeWhenInfoLogAttributeIsAddedToFiltersUsingNonGenericAddServiceMethod()
        {
            await TestAssistants.VerifyAnalyzerAsync<AddInfoLogAttributeAnalyzer>(@"
using Microsoft.Extensions.DependencyInjection;
using RockLib.Logging.AspNetCore;

public class TestController
{
    public string Get() => ""Get"";
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.AddService(typeof(InfoLogAttribute));
        });
    }
}" + _aspNetCoreStub).ConfigureAwait(false);
        }
    }
}
