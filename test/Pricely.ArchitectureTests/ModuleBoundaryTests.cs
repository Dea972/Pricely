using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Pricely.Modules.AiEngine.Infrastructure;
using Pricely.Modules.Catalog.Infrastructure;
using Pricely.Modules.Pricing.Infrastructure;
using Pricely.Modules.Users.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// Modules never reference each other, except another module's IntegrationEvents or Contracts project (ADR-022).
/// </summary>
public sealed class ModuleBoundaryTests
{
    private const string CatalogNamespace = "Pricely.Modules.Catalog";
    private const string PricingNamespace = "Pricely.Modules.Pricing";
    private const string UsersNamespace = "Pricely.Modules.Users";
    private const string AiEngineNamespace = "Pricely.Modules.AiEngine";
    private const string AgenticNamespace = "Pricely.Modules.Agentic";

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

    private static readonly Assembly[] AiEngineAssemblies =
    [
        typeof(Modules.AiEngine.Domain.Models.Model).Assembly,
        Modules.AiEngine.Application.AssemblyReference.Assembly,
        typeof(AiEngineModule).Assembly,
        Modules.AiEngine.Presentation.AssemblyReference.Assembly
    ];

    private static readonly Assembly[] AgenticAssemblies =
    [
        typeof(Modules.Agentic.Domain.Conversations.AgentConversation).Assembly,
        Modules.Agentic.Application.AssemblyReference.Assembly,
        typeof(Modules.Agentic.Infrastructure.AgenticModule).Assembly,
        Modules.Agentic.Presentation.AssemblyReference.Assembly
    ];

    private static readonly string[] CatalogInternals = Internals(CatalogNamespace);
    private static readonly string[] PricingInternals = Internals(PricingNamespace);
    private static readonly string[] UsersInternals = Internals(UsersNamespace);
    private static readonly string[] AiEngineInternals = Internals(AiEngineNamespace);

    [Fact]
    public void CatalogModule_ShouldNotDependOn_OtherModules()
    {
        AssertNoDependency(CatalogAssemblies, [.. PricingInternals, PricingNamespace + ".IntegrationEvents"]);
        AssertNoDependency(CatalogAssemblies, [.. UsersInternals, UsersNamespace + ".IntegrationEvents"]);
    }

    [Fact]
    public void PricingModule_ShouldOnlyDependOn_CatalogIntegrationEventsAndContracts()
    {
        AssertNoDependency(PricingAssemblies, CatalogInternals);
        AssertNoDependency(PricingAssemblies, UsersInternals);
    }

    [Fact]
    public void UsersModule_ShouldNotDependOn_OtherModules()
    {
        AssertNoDependency(UsersAssemblies, [.. CatalogInternals, CatalogNamespace + ".IntegrationEvents", CatalogNamespace + ".Contracts"]);
        AssertNoDependency(UsersAssemblies, [.. PricingInternals, PricingNamespace + ".IntegrationEvents"]);
    }

    [Fact]
    public void AiEngineModule_ShouldNotDependOn_OtherModules()
    {
        AssertNoDependency(AiEngineAssemblies, [.. CatalogInternals, CatalogNamespace + ".IntegrationEvents", CatalogNamespace + ".Contracts"]);
        AssertNoDependency(AiEngineAssemblies, [.. PricingInternals, PricingNamespace + ".IntegrationEvents"]);
        AssertNoDependency(AiEngineAssemblies, [.. UsersInternals, UsersNamespace + ".IntegrationEvents"]);
    }

    [Fact]
    public void OtherModules_ShouldNotDependOn_AiEngine()
    {
        AssertNoDependency(CatalogAssemblies, AiEngineInternals);
        AssertNoDependency(PricingAssemblies, AiEngineInternals);
        AssertNoDependency(UsersAssemblies, AiEngineInternals);
    }

    /// <summary>
    /// Agentic is generic (ADR-014): it knows no other module nor the agents, and no other module knows it (agents and tools are
    /// declared through Common).
    /// </summary>
    [Fact]
    public void AgenticModule_ShouldNotDependOn_OtherModules_NorBeDependedOn()
    {
        AssertNoDependency(AgenticAssemblies, [CatalogNamespace, PricingNamespace, UsersNamespace, AiEngineNamespace, "Pricely.Agents"]);
        AssertNoDependency([.. CatalogAssemblies, .. PricingAssemblies, .. UsersAssemblies, .. AiEngineAssemblies], [AgenticNamespace]);
    }

    [Fact]
    public void DomainLayers_ShouldNotDependOn_PersistenceOrMessaging()
    {
        string[] forbidden = ["Microsoft.EntityFrameworkCore", "MassTransit", "Microsoft.AspNetCore", "Dapper", "Npgsql", "Microsoft.Extensions.AI"];

        TestResult result = Types
            .InAssemblies([CatalogAssemblies[0], PricingAssemblies[0], UsersAssemblies[0], AiEngineAssemblies[0], AgenticAssemblies[0]])
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
