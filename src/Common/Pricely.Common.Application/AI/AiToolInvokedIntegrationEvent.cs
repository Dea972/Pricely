using Pricely.Common.Application.EventBus;

namespace Pricely.Common.Application.AI;

/// <summary>
/// Published after every tool call made for an agent (by the model, or after a person approved it). Consumed by the
/// AiEngine module for its journal. Published directly on the bus, like <see cref="LlmCalledIntegrationEvent"/>.
/// </summary>
public sealed class AiToolInvokedIntegrationEvent : IntegrationEvent
{
    public AiToolInvokedIntegrationEvent(Guid id, DateTime occurredOnUtc)
        : base(id, occurredOnUtc)
    {
    }

    /// <summary>Usage session (the conversation), as in <see cref="LlmCalledIntegrationEvent.SessionId"/>.</summary>
    public required string SessionId { get; init; }

    public required string FeatureCode { get; init; }

    public required string SourceModule { get; init; }

    public required string AgentName { get; init; }

    public required string ToolName { get; init; }

    public bool IsReadOnly { get; init; }

    /// <summary>True when the call ran after a person approved it.</summary>
    public bool WasApproved { get; init; }

    public string? UserId { get; init; }

    public long DurationMs { get; init; }

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }
}
