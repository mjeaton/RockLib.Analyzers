using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RockLib.Logging.AspNetCore.Analyzers.Test
{
    internal static class TestAssistants
    {
        // NOTE: The code provided to source may contain the "[|" and "|]"
        // character combinations. This indicates that the code within these
        // combinations is in error, and the analyzer should identify this span
        // with a diagnostic.
        // For more information, visit https://github.com/dotnet/roslyn-sdk/blob/main/src/Microsoft.CodeAnalysis.Testing/README.md
        public static async Task VerifyAnalyzerAsync<TAnalyzer>(string source)
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            var test = new CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Default
                    .AddPackages(ImmutableArray.Create(
                        new PackageIdentity("RockLib.Logging.AspNetCore", "3.2.0"),
                        new PackageIdentity("Microsoft.AspNetCore.Mvc", "2.2.0")))
            };

            await test.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public static async Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(string source, string fixedSource)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : CodeFixProvider, new()
        {
            var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
            {
                TestCode = source,
                FixedCode = fixedSource,
                ReferenceAssemblies = ReferenceAssemblies.Default
                    .AddPackages(ImmutableArray.Create(
                        new PackageIdentity("RockLib.Logging.AspNetCore", "3.2.0"),
                        new PackageIdentity("Microsoft.AspNetCore.Mvc", "2.2.0")))
            };

            await test.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}