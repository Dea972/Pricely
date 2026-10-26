using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.AgentActivities;

/// <summary>
/// What an agent did besides calling the model, immutable once recorded: a tool call or a decision on a proposed action.
/// Next to <c>LlmCall</c>, it is the source of the agent statistics.
/// </summary>
public sealed class AgentActivity : Entity
{
    public const int SessionIdMaxLength = 100;

    public const int FeatureCodeMaxLength = 100;

    public const int AgentNameMaxLength = 50;

    public const int ToolNameMaxLength = 100;

    public const int UserIdMaxLength = 100;

    public const int ErrorMessageMaxLength = 2000;

    private AgentActivity()
    {
    }

    public Guid Id { get; private set; }

    public AgentActivityKind Kind { get; private set; }

    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>Usage session (the conversation): links the activity to the <c>LlmCall</c>s of the same session.</summary>
    public string SessionId { get; private set; } = string.Empty;

    public string FeatureCode { get; private set; } = string.Empty;

    public string AgentName { get; private set; } = string.Empty;

    public string ToolName { get; private set; } = string.Empty;

    public string? UserId { get; private set; }

    public bool IsReadOnlyTool { get; private set; }

    /// <summary>Tool call made after a person approved it.</summary>
    public bool WasApproved { get; private set; }

    public long? DurationMs { get; private set; }

    public bool IsSuccess { get; private set; }

    public string? ErrorMessage { get; private set; }

    public AgentActionDecision? Decision { get; private set; }

    /// <summary>For a decision: the action of the Agentic module (plain identifier, no foreign key across schemas).</summary>
    public Guid? ActionId { get; private set; }

    /// <summary>For a decision: time between the proposal and the person's decision (the tool run is not included).</summary>
    public long? DecisionDelayMs { get; private set; }

    public static AgentActivity RecordToolInvocation(AgentActivityDetails details, bool isReadOnly, bool wasApproved, long durationMs, string? errorMessage) =>
        new()
        {
            Id = details.Id,
            Kind = AgentActivityKind.ToolInvoked,
            OccurredOnUtc = details.OccurredOnUtc,
            SessionId = Truncate(details.SessionId, SessionIdMaxLength),
            FeatureCode = Truncate(details.FeatureCode, FeatureCodeMaxLength),
            AgentName = Truncate(details.AgentName, AgentNameMaxLength),
            ToolName = Truncate(details.ToolName, ToolNameMaxLength),
            UserId = details.UserId is null ? null : Truncate(details.UserId, UserIdMaxLength),
            IsReadOnlyTool = isReadOnly,
            WasApproved = wasApproved,
            DurationMs = durationMs,
            IsSuccess = errorMessage is null,
            ErrorMessage = errorMessage is null ? null : Truncate(errorMessage, ErrorMessageMaxLength)
        };

    public static AgentActivity RecordDecision(AgentActivityDetails details, Guid actionId, AgentActionDecision decision, DateTime proposedOnUtc) =>
        new()
        {
            Id = details.Id,
            Kind = AgentActivityKind.ActionDecided,
            OccurredOnUtc = details.OccurredOnUtc,
            SessionId = Truncate(details.SessionId, SessionIdMaxLength),
            FeatureCode = Truncate(details.FeatureCode, FeatureCodeMaxLength),
            AgentName = Truncate(details.AgentName, AgentNameMaxLength),
            ToolName = Truncate(details.ToolName, ToolNameMaxLength),
            UserId = details.UserId is null ? null : Truncate(details.UserId, UserIdMaxLength),
            WasApproved = decision != AgentActionDecision.Rejected,
            IsSuccess = decision != AgentActionDecision.Failed,
            Decision = decision,
            ActionId = actionId,
            DecisionDelayMs = Math.Max(0, (long)(details.OccurredOnUtc - proposedOnUtc).TotalMilliseconds)
        };

    /// <summary>A journal entry is never lost because a reported value is too long: it is cut to the column size.</summary>
    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
