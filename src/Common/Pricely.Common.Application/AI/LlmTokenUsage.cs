namespace Pricely.Common.Application.AI;

/// <summary>
/// Tokens consumed by a call, as reported by the provider.
/// </summary>
public sealed record LlmTokenUsage(
    int InputTokens,
    int InputCachedTokens,
    int InputCacheCreationTokens,
    int OutputTokens,
    int OutputReasoningTokens)
{
    public static readonly LlmTokenUsage None = new(0, 0, 0, 0, 0);

    public int TotalTokens => InputTokens + InputCachedTokens + InputCacheCreationTokens + OutputTokens;
}
