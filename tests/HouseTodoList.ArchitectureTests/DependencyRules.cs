using System.Reflection;
using Mono.Cecil;
using NetArchTest.Rules;

namespace HouseTodoList.ArchitectureTests;

/// <summary>
/// The dependency rules in ARCHITECTURE.md. Each test is one rule; a failure lists the types that break it.
/// </summary>
public class DependencyRules
{
    private const string CoreName = "HouseTodoList.Core";
    private const string AdaptersName = "HouseTodoList.Adapters";
    private const string WebName = "HouseTodoList.Web";
    private const string CompositionRoot = WebName + ".Program";

    // Boundary libraries: only HouseTodoList.Adapters (storage) and HouseTodoList.Web (HTTP, sign-in) may use them.
    private static readonly string[] StorageNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "SQLitePCL",
    ];

    private static readonly string[] HostNamespaces =
    [
        "Microsoft.AspNetCore",
        "System.Net.Http",
    ];

    private static readonly Assembly Core = Assembly.Load(CoreName);
    private static readonly Assembly Adapters = Assembly.Load(AdaptersName);
    private static readonly Assembly Web = Assembly.Load(WebName);

    [Fact]
    public void CoreDoesNotDependOnAdaptersOrWeb()
    {
        var result = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOnAny(AdaptersName, WebName)
            .GetResult();

        AssertRule(result, $"{CoreName} must not depend on {AdaptersName} or {WebName}.");
    }

    [Fact]
    public void CoreDoesNotDependOnStorageOrHostLibraries()
    {
        var result = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOnAny([.. StorageNamespaces, .. HostNamespaces])
            .GetResult();

        AssertRule(
            result,
            $"{CoreName} must not use EF Core, SQLite, ASP.NET Core or HttpClient. Put boundary code in {AdaptersName} behind a port in {CoreName}.");
    }

    [Fact]
    public void AdaptersDoNotDependOnWeb()
    {
        var result = Types.InAssembly(Adapters)
            .ShouldNot().HaveDependencyOnAny([WebName, .. HostNamespaces])
            .GetResult();

        AssertRule(result, $"{AdaptersName} must not depend on {WebName} or ASP.NET Core.");
    }

    [Fact]
    public void OnlyTheCompositionRootInWebDependsOnAdapters()
    {
        var result = Types.InAssembly(Web)
            .That().MeetCustomRule(new IsNotCompositionRoot())
            .ShouldNot().HaveDependencyOn(AdaptersName)
            .GetResult();

        AssertRule(
            result,
            $"Only {CompositionRoot} (the composition root) may use {AdaptersName}. Pages and other types use {CoreName}'s use cases and ports.");
    }

    [Fact]
    public void WebDoesNotDependOnStorageLibraries()
    {
        var result = Types.InAssembly(Web)
            .ShouldNot().HaveDependencyOnAny(StorageNamespaces)
            .GetResult();

        AssertRule(
            result,
            $"{WebName} must not use EF Core or SQLite directly. Register and migrate storage through an extension method in {AdaptersName}.");
    }

    private static void AssertRule(TestResult result, string rule)
    {
        var offenders = string.Join(", ", result.FailingTypeNames ?? []);
        Assert.True(result.IsSuccessful, $"{rule} Offending types: {offenders}");
    }

    /// <summary>Matches every type except the composition root and the types the compiler nests inside it.</summary>
    private sealed class IsNotCompositionRoot : ICustomRule
    {
        public bool MeetsRule(TypeDefinition type)
        {
            var outermost = type;
            while (outermost.DeclaringType is not null)
            {
                outermost = outermost.DeclaringType;
            }

            return outermost.FullName != CompositionRoot;
        }
    }
}
