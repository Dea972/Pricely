namespace Pricely.Common.Application.AI;

/// <summary>
/// A prompt resolved for a provider: model chosen and feature metadata attached (for usage tracking).
/// </summary>
public sealed class LlmRequest
{
    public required AiFeature Feature { get; init; }

    public required LlmPrompt Prompt { get; init; }

    /// <summary>Model of the provider; the provider's default model when null.</summary>
    public string? Model { get; init; }

    public required string SessionId { get; init; }
}
