using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Catalog.Infrastructure;

namespace Pricely.ArchitectureTests;

/// <summary>
/// A module contract (<c>Pricely.Modules.&lt;M&gt;.Contracts</c>, ADR-022) is the read surface a module offers to the
/// Application layer of the others: interfaces and immutable records on Common.Domain only, implemented internally by the
/// owner.
/// </summary>
public sealed class ContractsTests
{
    private const string CatalogContractsNamespace = "Pricely.Modules.Catalog.Contracts";

    // One entry per module that offers a contract.
    private static readonly Assembly[] ContractAssemblies = [typeof(IProductCategoryReader).Assembly];

    [Fact]
    public void Contracts_ShouldOnlyReference_CommonDomain()
    {
        foreach (Assembly contracts in ContractAssemblies)
        {
            contracts.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(name => name!.StartsWith("Pricely.", StringComparison.Ordinal))
                .Should().BeEquivalentTo(["Pricely.Common.Domain"], contracts.GetName().Name);
        }
    }

    [Fact]
    public void Contracts_ShouldOnlyContain_InterfacesImmutableRecordsAndErrors()
    {
        foreach (Type type in ContractAssemblies.SelectMany(a => a.GetExportedTypes()))
        {
            bool isStaticClass = type is { IsAbstract: true, IsSealed: true };
            bool isRecord = type.GetMethod("<Clone>$") is not null;

            (type.IsInterface || type.IsEnum || isStaticClass || isRecord && type.IsSealed)
                .Should().BeTrue("{0} must be an interface, an enum, a static class of errors or a sealed record", type.FullName);

            foreach (PropertyInfo property in type.GetProperties().Where(p => !type.IsInterface && p.SetMethod is not null))
            {
                property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers()
                    .Should().Contain(typeof(IsExternalInit), "{0}.{1} must be init-only", type.FullName, property.Name);
            }
        }
    }

    [Fact]
    public void ContractMethods_ShouldBeAsync_ReturnAResult_AndTakeACancellationToken()
    {
        IEnumerable<MethodInfo> methods = ContractAssemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsInterface)
            .SelectMany(t => t.GetMethods())
            .Where(m => !m.IsStatic);

        foreach (MethodInfo method in methods)
        {
            Type returnType = method.ReturnType;
            returnType.IsGenericType.Should().BeTrue("{0}.{1} returns a Task<Result>", method.DeclaringType!.Name, method.Name);
            returnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>), method.Name);

            Type result = returnType.GetGenericArguments()[0];
            (result == typeof(Result) || result.IsGenericType && result.GetGenericTypeDefinition() == typeof(Result<>))
                .Should().BeTrue("{0}.{1} reports a failure as a Result, never an exception", method.DeclaringType!.Name, method.Name);

            ParameterInfo[] parameters = method.GetParameters();
            parameters[^1].ParameterType.Should().Be<CancellationToken>(method.Name);
        }
    }

    [Fact]
    public void ContractImplementations_ShouldBeInternal_AndLiveInTheOwnerInfrastructure()
    {
        Type[] implementations = typeof(CatalogModule).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i => i.Namespace?.StartsWith(CatalogContractsNamespace, StringComparison.Ordinal) == true))
            .ToArray();

        implementations.Should().NotBeEmpty();
        implementations.Should().OnlyContain(t => !t.IsPublic && t.IsSealed, "a contract is reached through its interface only");
    }

    /// <summary>
    /// The contract is read by Application (the layer that holds the use cases); Domain stays pure and Presentation goes
    /// through Application.
    /// </summary>
    [Fact]
    public void ContractConsumers_ShouldUseTheContractFromApplicationOnly()
    {
        Assembly[] pricingDomainAndPresentation =
        [
            typeof(Modules.Pricing.Domain.Offers.Offer).Assembly,
            Modules.Pricing.Presentation.AssemblyReference.Assembly
        ];

        NetArchTest.Rules.TestResult result = NetArchTest.Rules.Types
            .InAssemblies(pricingDomainAndPresentation)
            .ShouldNot()
            .HaveDependencyOnAny(CatalogContractsNamespace)
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }
}
