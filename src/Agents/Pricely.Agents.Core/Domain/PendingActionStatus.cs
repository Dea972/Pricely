namespace Pricely.Agents.Core.Domain;

public enum PendingActionStatus
{
    /// <summary>Proposed by the agent, waiting for the user's decision.</summary>
    Pending = 0,

    /// <summary>Approved and run successfully.</summary>
    Executed = 1,

    /// <summary>Approved, but the tool failed.</summary>
    Failed = 2,

    /// <summary>Rejected by the user: nothing was changed.</summary>
    Rejected = 3
}
