using Pricely.Common.Application.EventBus;

namespace Pricely.Common.Application.AI;

/// <summary>
/// Published (through the outbox of the Agentic module) when a person approves or rejects an action proposed by an
/// agent, once its outcome is known. Consumed by the AiEngine module for its journal.
/// </summary>
public sealed class AiActionDecidedIntegrationEvent : IntegrationEvent
{
    public const string Executed = "Executed";
    public const string Failed = "Failed";
    public const string Rejected = "Rejected";

    public AiActionDecidedIntegrationEvent(Guid id, DateTime occurredOnUtc)
        : base(id, occurredOnUtc)
    {
    }

    public required Guid ActionId { get; init; }

    public required string SessionId { get; init; }

    public required string FeatureCode { get; init; }

    public required string AgentName { get; init; }

    public required string ToolName { get; init; }

    /// <summary><see cref="Executed"/>, <see cref="Failed"/> or <see cref="Rejected"/>.</summary>
    public required string Decision { get; init; }

    public string? UserId { get; init; }

    public required DateTime ProposedOnUtc { get; init; }
}
