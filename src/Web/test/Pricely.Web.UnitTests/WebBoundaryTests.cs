using System.Reflection;
using FluentAssertions;
using Pricely.Web.Api;

namespace Pricely.Web.UnitTests;

/// <summary>
/// The front talks to the API over HTTP only (ADR-015): it references no module and no Common project, so a change of a
/// module's internals can never reach it other than through the API.
/// </summary>
public sealed class WebBoundaryTests
{
    [Fact]
    public void Front_ShouldReferenceNoModule_NorCommon()
    {
        IEnumerable<string?> references = typeof(PricelyApiClient).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        references.Should().NotContain(name => name!.StartsWith("Pricely.Modules.", StringComparison.Ordinal)
            || name.StartsWith("Pricely.Common.", StringComparison.Ordinal)
            || name.StartsWith("Pricely.Agents.", StringComparison.Ordinal)
            || name == "Pricely.Sdk");
    }

    /// <summary>A project reference whose types are not used yet would not appear in the compiled references.</summary>
    [Fact]
    public void FrontProject_ShouldOnlyReferenceTheServiceDefaults()
    {
        string project = File.ReadAllText(FindFile("src/Web/Pricely.Web/Pricely.Web.csproj"));

        System.Text.RegularExpressions.Regex.Matches(project, "<ProjectReference Include=\"([^\"]+)\"")
            .Select(m => Path.GetFileName(m.Groups[1].Value.Replace('\\', '/')))
            .Should().Equal("Pricely.ServiceDefaults.csproj");
    }

    [Fact]
    public void Front_ShouldNotUseEfCore_NorMediatR()
    {
        Assembly front = typeof(PricelyApiClient).Assembly;

        front.GetReferencedAssemblies().Select(a => a.Name).Should().NotContain(name =>
            name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || name.StartsWith("MediatR", StringComparison.Ordinal)
            || name.StartsWith("Npgsql", StringComparison.Ordinal));
    }

    private static string FindFile(string relativePath)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"'{relativePath}' was not found above the test directory.");
    }
}
