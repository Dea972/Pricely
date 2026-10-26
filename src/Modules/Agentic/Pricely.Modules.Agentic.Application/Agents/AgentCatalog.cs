using Pricely.Common.Application.AI.Agents;

namespace Pricely.Modules.Agentic.Application.Agents;

/// <summary>
/// The agents and the tools declared by the application (<c>AddAgent</c>, <c>AddAgentTools</c>): the module itself knows
/// none of them.
/// </summary>
public sealed class AgentCatalog(IEnumerable<AgentDefinition> agents, IEnumerable<AgentTool> tools, AgenticOptions options)
{
    private readonly Dictionary<string, AgentTool> _tools = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);

    public AgentDefinition? FindAgent(string name) =>
        agents.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    public AgentTool? FindTool(string name) => _tools.GetValueOrDefault(name);

    /// <summary>The tools an agent may use, among those that exist.</summary>
    public IReadOnlyCollection<AgentTool> ToolsOf(AgentDefinition agent) =>
        agent.ToolNames.Select(FindTool).OfType<AgentTool>().ToList();

    public bool IsToolOf(string agentName, string toolName) =>
        FindAgent(agentName)?.ToolNames.Contains(toolName, StringComparer.Ordinal) == true;

    /// <summary>Scope needed to approve a write tool: its own, or the module's default.</summary>
    public string ApprovalScopeOf(AgentTool tool) => tool.ApprovalScope ?? options.DefaultApprovalScope;
}
