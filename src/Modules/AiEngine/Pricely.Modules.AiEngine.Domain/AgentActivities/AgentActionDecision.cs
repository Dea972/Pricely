namespace Pricely.Modules.AiEngine.Domain.AgentActivities;

/// <summary>Outcome of a person's decision on an action proposed by an agent.</summary>
public enum AgentActionDecision
{
    Executed = 0,
    Failed = 1,
    Rejected = 2
}
