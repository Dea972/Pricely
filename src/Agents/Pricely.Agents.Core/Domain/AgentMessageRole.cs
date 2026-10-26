namespace Pricely.Agents.Core.Domain;

public enum AgentMessageRole
{
    User = 0,
    Assistant = 1,
    Tool = 2,

    /// <summary>Note for the model only (an approved or rejected action), not shown as a chat bubble.</summary>
    System = 3
}
