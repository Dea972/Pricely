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

    [Fact]
    public void Front_ShouldNotUseEfCore_NorMediatR()
    {
        Assembly front = typeof(PricelyApiClient).Assembly;

        front.GetReferencedAssemblies().Select(a => a.Name).Should().NotContain(name =>
            name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || name.StartsWith("MediatR", StringComparison.Ordinal)
            || name.StartsWith("Npgsql", StringComparison.Ordinal));
    }
}
