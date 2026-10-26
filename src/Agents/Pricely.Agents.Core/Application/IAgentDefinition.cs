using Pricely.Common.Application.AI;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// An agent of Pricely: its name (route), the AI feature it calls the LLM for, its instructions and the tools it may use.
/// Each agent lives in its own project under <c>src/Agents</c> and registers itself with <c>AddAgent</c>.
/// </summary>
public interface IAgentDefinition
{
    /// <summary>Route segment, kebab-case (for example <c>assistant</c>).</summary>
    string Name { get; }

    AiFeature Feature { get; }

    /// <summary>System prompt.</summary>
    string Instructions { get; }

    /// <summary>Names of the tools (<c>[McpServerTool(Name = ...)]</c>) the agent may call.</summary>
    IReadOnlyCollection<string> ToolNames { get; }
}
