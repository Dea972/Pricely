using Pricely.Common.Application.EventBus;

namespace Pricely.Common.Application.AI;

/// <summary>
/// Published after every LLM call (success or failure) by <see cref="LlmServiceBase"/>.
/// Consumed by the AiEngine module to keep the call journal and its costs.
/// </summary>
public sealed class LlmCalledIntegrationEvent : IntegrationEvent
{
    public LlmCalledIntegrationEvent(Guid id, DateTime occurredOnUtc)
        : base(id, occurredOnUtc)
    {
    }

    public required string SessionId { get; init; }

    public required string FeatureCode { get; init; }

    public required string SourceModule { get; init; }

    public string? UserId { get; init; }

    public required string ModelProvider { get; init; }

    public required string ModelName { get; init; }

    public int InputTokens { get; init; }

    public int InputCachedTokens { get; init; }

    public int InputCacheCreationTokens { get; init; }

    public int OutputTokens { get; init; }

    public int OutputReasoningTokens { get; init; }

    public long ResponseTimeMs { get; init; }

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }
}
