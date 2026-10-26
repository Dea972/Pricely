namespace Pricely.Modules.AiEngine.Domain.AgentActivities;

public enum AgentActivityKind
{
    /// <summary>A tool ran for an agent (read tool during a turn, or write tool after approval).</summary>
    ToolInvoked = 0,

    /// <summary>A person decided on an action proposed by an agent.</summary>
    ActionDecided = 1
}
