namespace Pricely.Modules.AiEngine.Domain.AgentActivities;

/// <summary>
/// What every agent activity reports: when, in which session, for which feature, agent and tool, for whom.
/// </summary>
public sealed record AgentActivityDetails(
    DateTime OccurredOnUtc,
    string SessionId,
    string FeatureCode,
    string AgentName,
    string ToolName,
    string? UserId);
