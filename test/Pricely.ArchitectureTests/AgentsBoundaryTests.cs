using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Pricely.Agents.Assistant;
using Pricely.Agents.Core;

namespace Pricely.ArchitectureTests;

/// <summary>
/// The agents (src/Agents, ADR-013) use the modules only through their tools, given by the composition root:
/// they never reference a module.
/// </summary>
public sealed class AgentsBoundaryTests
{
    private static readonly Assembly[] AgentAssemblies =
    [
        typeof(AgentsModule).Assembly,
        typeof(AssistantAgent).Assembly
    ];

    [Fact]
    public void Agents_ShouldNotDependOn_AnyModule()
    {
        TestResult result = Types
            .InAssemblies(AgentAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny("Pricely.Modules")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void AgentProjects_ShouldOnlyReference_TheAgentsCore()
    {
        typeof(AssistantAgent).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(name => name!.StartsWith("Pricely.", StringComparison.Ordinal))
            .Should().BeEquivalentTo("Pricely.Agents.Core", "Pricely.Common.Application");
    }
}
