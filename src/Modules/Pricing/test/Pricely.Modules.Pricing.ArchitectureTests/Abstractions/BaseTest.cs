using System.Reflection;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Infrastructure;

namespace Pricely.Modules.Pricing.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Pricing.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(Offer).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(PricingModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Pricing.Presentation.AssemblyReference).Assembly;
}
