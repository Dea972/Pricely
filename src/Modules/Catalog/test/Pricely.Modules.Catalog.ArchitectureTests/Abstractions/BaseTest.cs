using System.Reflection;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.Infrastructure;

namespace Pricely.Modules.Catalog.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Catalog.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(Product).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(CatalogModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Catalog.Presentation.AssemblyReference).Assembly;
}
