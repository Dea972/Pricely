namespace Pricely.Common.Application.AI;

/// <summary>
/// Keys of <c>UsageDetails.AdditionalCounts</c> that providers fill in and the call journal reads.
/// </summary>
public static class LlmChatUsage
{
    /// <summary>Tokens written to the prompt cache.</summary>
    public const string CacheCreationInputTokens = "cache_creation_input_tokens";
}
