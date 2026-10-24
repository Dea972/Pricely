using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Pricely.Modules.Catalog.Infrastructure;
using Pricely.Modules.Pricing.Infrastructure;
using Pricely.Modules.Users.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// Modules never reference each other, except another module's IntegrationEvents project.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private const string CatalogNamespace = "Pricely.Modules.Catalog";
    private const string PricingNamespace = "Pricely.Modules.Pricing";
    private const string UsersNamespace = "Pricely.Modules.Users";

    private static readonly Assembly[] CatalogAssemblies =
    [
        typeof(Modules.Catalog.Domain.Products.Product).Assembly,
        Modules.Catalog.Application.AssemblyReference.Assembly,
        typeof(CatalogModule).Assembly,
        Modules.Catalog.Presentation.AssemblyReference.Assembly
    ];

    private static readonly Assembly[] PricingAssemblies =
    [
        typeof(Modules.Pricing.Domain.Offers.Offer).Assembly,
        Modules.Pricing.Application.AssemblyReference.Assembly,
        typeof(PricingModule).Assembly,
        Modules.Pricing.Presentation.AssemblyReference.Assembly
    ];

    private static readonly Assembly[] UsersAssemblies =
    [
        typeof(Modules.Users.Domain.Users.User).Assembly,
        Modules.Users.Application.AssemblyReference.Assembly,
        typeof(UsersModule).Assembly,
        Modules.Users.Presentation.AssemblyReference.Assembly
    ];

    private static readonly string[] CatalogInternals = Internals(CatalogNamespace);
    private static readonly string[] PricingInternals = Internals(PricingNamespace);
    private static readonly string[] UsersInternals = Internals(UsersNamespace);

    [Fact]
    public void CatalogModule_ShouldNotDependOn_OtherModules()
    {
        AssertNoDependency(CatalogAssemblies, [.. PricingInternals, PricingNamespace + ".IntegrationEvents"]);
        AssertNoDependency(CatalogAssemblies, [.. UsersInternals, UsersNamespace + ".IntegrationEvents"]);
    }

    [Fact]
    public void PricingModule_ShouldOnlyDependOn_CatalogIntegrationEvents()
    {
        AssertNoDependency(PricingAssemblies, CatalogInternals);
        AssertNoDependency(PricingAssemblies, UsersInternals);
    }

    [Fact]
    public void UsersModule_ShouldNotDependOn_OtherModules()
    {
        AssertNoDependency(UsersAssemblies, [.. CatalogInternals, CatalogNamespace + ".IntegrationEvents"]);
        AssertNoDependency(UsersAssemblies, [.. PricingInternals, PricingNamespace + ".IntegrationEvents"]);
    }

    [Fact]
    public void DomainLayers_ShouldNotDependOn_PersistenceOrMessaging()
    {
        string[] forbidden = ["Microsoft.EntityFrameworkCore", "MassTransit", "Microsoft.AspNetCore", "Dapper"];

        TestResult result = Types
            .InAssemblies([CatalogAssemblies[0], PricingAssemblies[0]])
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    private static string[] Internals(string module) =>
    [
        module + ".Domain",
        module + ".Application",
        module + ".Infrastructure",
        module + ".Presentation"
    ];

    private static void AssertNoDependency(Assembly[] assemblies, string[] forbiddenNamespaces)
    {
        TestResult result = Types
            .InAssemblies(assemblies)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }
}
