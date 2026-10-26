using FluentAssertions;
using Pricely.Agents.Assistant;

namespace Pricely.ArchitectureTests;

/// <summary>
/// The agents of Pricely (src/Agents) are declarations on Common only (ADR-014): no module, not even Agentic.
/// </summary>
public sealed class AgentsBoundaryTests
{
    [Fact]
    public void AgentProjects_ShouldOnlyReference_Common()
    {
        typeof(AssistantAgent).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(name => name!.StartsWith("Pricely.", StringComparison.Ordinal))
            .Should().BeEquivalentTo("Pricely.Common.Application");
    }
}
