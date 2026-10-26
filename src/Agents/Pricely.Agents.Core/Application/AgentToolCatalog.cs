using System.Reflection;
using Pricely.Common.Presentation.Mcp;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// Every tool declared by the modules (the same definitions as the MCP server, ADR-009), by name.
/// The module tool assemblies are given by the composition root (<c>Pricely.Sdk</c>): the agents reference no module.
/// </summary>
public sealed class AgentToolCatalog
{
    private readonly Dictionary<string, AgentTool> _tools;

    public AgentToolCatalog(IEnumerable<Assembly> toolAssemblies)
    {
        _tools = McpServerExtensions.ToolMethods(toolAssemblies)
            .Select(AgentTool.FromMethod)
            .ToDictionary(tool => tool.Name, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<AgentTool> All => _tools.Values;

    public AgentTool? Find(string name) => _tools.GetValueOrDefault(name);
}
