using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Pricely.Common.Application.Messaging;
using Pricely.Modules.AiEngine.Infrastructure;
using Pricely.Modules.Catalog.Infrastructure;
using Pricely.Modules.Pricing.Infrastructure;
using Pricely.Modules.Users.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// Query handlers live in the Application layer of their module, whatever they read with (Dapper and
/// <c>IDbConnectionFactory</c>, a domain repository): Infrastructure only implements ports.
/// </summary>
public sealed class QueryHandlerTests
{
    private static readonly Assembly[] InfrastructureAssemblies =
    [
        typeof(CatalogModule).Assembly,
        typeof(PricingModule).Assembly,
        typeof(UsersModule).Assembly,
        typeof(AiEngineModule).Assembly,
        typeof(Modules.Agentic.Infrastructure.AgenticModule).Assembly
    ];

    private static readonly Assembly[] ApplicationAssemblies =
    [
        Modules.Catalog.Application.AssemblyReference.Assembly,
        Modules.Pricing.Application.AssemblyReference.Assembly,
        Modules.Users.Application.AssemblyReference.Assembly,
        Modules.AiEngine.Application.AssemblyReference.Assembly,
        Modules.Agentic.Application.AssemblyReference.Assembly
    ];

    [Fact]
    public void QueryHandlers_ShouldNotBeInInfrastructure()
    {
        TestResult result = Types
            .InAssemblies(InfrastructureAssemblies)
            .ShouldNot()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Application_ShouldNotDependOn_DataAccessLibraries()
    {
        TestResult result = Types
            .InAssemblies(ApplicationAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Dapper", "Npgsql")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    /// <summary>
    /// A <c>ReadStore</c> is a port of Application (an interface) implemented once, internally and sealed, by the
    /// Infrastructure of the same module (ADR-024).
    /// </summary>
    [Fact]
    public void ReadStores_ShouldBeImplementedOnceAndInternally_ByInfrastructure()
    {
        Type[] ports = [.. ApplicationAssemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsInterface && t.Name.EndsWith("ReadStore", StringComparison.Ordinal))];

        ports.Should().NotBeEmpty();

        foreach (Type port in ports)
        {
            Type[] implementations = [.. InfrastructureAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && port.IsAssignableFrom(t))];

            implementations.Should().ContainSingle("{0} has exactly one implementation", port.Name);
            implementations[0].Should().Match<Type>(t => !t.IsPublic && t.IsSealed, "{0} is internal sealed", implementations[0].Name);
        }
    }
}
