using Pricely.Common.Application.AI.Agents;

namespace Pricely.Modules.Agentic.Application.Agents;

/// <summary>
/// The agents and the tools declared by the application (<c>AddAgent</c>, <c>AddAgentTools</c>): the module itself knows
/// none of them.
/// </summary>
public sealed class AgentCatalog(IEnumerable<AgentDefinition> agents, IEnumerable<AgentTool> tools, AgenticOptions options)
{
    private readonly Dictionary<string, AgentTool> _tools = Validate(agents, tools.ToDictionary(t => t.Name, StringComparer.Ordinal));

    public AgentDefinition? FindAgent(string name) =>
        agents.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    public AgentTool? FindTool(string name) => _tools.GetValueOrDefault(name);

    /// <summary>The tools an agent may use, among those that exist.</summary>
    public IReadOnlyCollection<AgentTool> ToolsOf(AgentDefinition agent) =>
        agent.ToolNames.Select(FindTool).OfType<AgentTool>().ToList();

    public bool IsToolOf(string agentName, string toolName) =>
        FindAgent(agentName)?.ToolNames.Contains(toolName, StringComparer.Ordinal) == true;

    /// <summary>
    /// A tool name of an agent that matches no tool is a mistake in the declaration: it fails as soon as the catalog is
    /// built (the startup composition test builds it).
    /// </summary>
    private static Dictionary<string, AgentTool> Validate(IEnumerable<AgentDefinition> agents, Dictionary<string, AgentTool> tools)
    {
        var unknown = agents
            .SelectMany(a => a.ToolNames.Where(name => !tools.ContainsKey(name)).Select(name => $"{a.Name}/{name}"))
            .ToList();

        return unknown.Count == 0
            ? tools
            : throw new InvalidOperationException($"Agents name tools that do not exist: {string.Join(", ", unknown)}.");
    }

    /// <summary>Scope needed to approve a write tool: its own, or the module's default.</summary>
    public string ApprovalScopeOf(AgentTool tool) => tool.ApprovalScope ?? options.DefaultApprovalScope;
}
