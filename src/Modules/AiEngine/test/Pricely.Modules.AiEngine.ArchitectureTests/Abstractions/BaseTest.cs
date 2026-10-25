using System.Reflection;
using Pricely.Modules.AiEngine.Domain.Models;
using Pricely.Modules.AiEngine.Infrastructure;

namespace Pricely.Modules.AiEngine.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(AiEngine.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(Model).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(AiEngineModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(AiEngine.Presentation.AssemblyReference).Assembly;
}
