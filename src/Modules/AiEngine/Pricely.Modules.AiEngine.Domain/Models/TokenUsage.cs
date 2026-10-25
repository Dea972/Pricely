namespace Pricely.Modules.AiEngine.Domain.Models;

/// <summary>Tokens consumed by a call, as reported by the model API.</summary>
public readonly record struct TokenUsage(
    int InputTokens,
    int InputCachedTokens,
    int InputCacheCreationTokens,
    int OutputTokens,
    int OutputReasoningTokens);

/// <summary>Cost of a call in USD, by price component.</summary>
public readonly record struct CallCost(decimal Input, decimal CachedInput, decimal CacheWrite, decimal Output)
{
    public decimal Total => Input + CachedInput + CacheWrite + Output;
}

/// <summary>Amounts of a price, in USD per million tokens.</summary>
public readonly record struct PriceValues(
    decimal InputPerMillion,
    decimal CachedInputPerMillion,
    decimal CacheWritePerMillion,
    decimal OutputPerMillion);
