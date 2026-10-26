using System.Reflection;
using Pricely.Modules.Agentic.Domain.Conversations;
using Pricely.Modules.Agentic.Infrastructure;

namespace Pricely.Modules.Agentic.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(Agentic.Application.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(AgentConversation).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(AgenticModule).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Agentic.Presentation.AssemblyReference).Assembly;
}
