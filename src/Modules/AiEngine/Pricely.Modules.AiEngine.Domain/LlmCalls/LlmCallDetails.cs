using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Domain.LlmCalls;

/// <summary>
/// What a provider reported about a call.
/// </summary>
public sealed class LlmCallDetails
{
    /// <summary>Groups the calls of one unit of work (conversation, collection run...), given by the caller.</summary>
    public required string SessionId { get; init; }

    public required DateTime CalledOnUtc { get; init; }

    public required string FeatureCode { get; init; }

    /// <summary>Module that declares the feature.</summary>
    public required string SourceModule { get; init; }

    public string? UserId { get; init; }

    public required string ModelProvider { get; init; }

    public required string ModelName { get; init; }

    public required TokenUsage TokenUsage { get; init; }

    public long ResponseTimeMs { get; init; }

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }
}
