using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Models;

/// <summary>
/// Price of a model in USD per million tokens, from <see cref="ValidFrom"/> (UTC day) until the next price.
/// </summary>
public sealed class ModelPrice : Entity
{
    private const decimal TokensPerUnit = 1_000_000m;
    private const int CostDecimals = 6;

    private ModelPrice()
    {
    }

    public Guid Id { get; private set; }

    public Guid ModelId { get; private set; }

    public DateOnly ValidFrom { get; private set; }

    public decimal InputPerMillion { get; private set; }

    /// <summary>Price of input tokens read from the provider cache.</summary>
    public decimal CachedInputPerMillion { get; private set; }

    /// <summary>Price of input tokens written to the provider cache.</summary>
    public decimal CacheWritePerMillion { get; private set; }

    public decimal OutputPerMillion { get; private set; }

    public PriceSource Source { get; private set; }

    public DateTime RecordedOnUtc { get; private set; }

    /// <summary>
    /// Cost of a call. Anthropic Messages API: cached and cache-creation tokens are reported apart from input tokens,
    /// reasoning is part of the output.
    /// </summary>
    public CallCost CostOf(TokenUsage usage) =>
        new(
            Cost(usage.InputTokens, InputPerMillion),
            Cost(usage.InputCachedTokens, CachedInputPerMillion),
            Cost(usage.InputCacheCreationTokens, CacheWritePerMillion),
            Cost(usage.OutputTokens, OutputPerMillion));

    internal static ModelPrice Create(Guid modelId, DateOnly validFrom, PriceValues values, PriceSource source, DateTime utcNow)
    {
        ModelPrice price = new()
        {
            Id = Guid.CreateVersion7(),
            ModelId = modelId,
            ValidFrom = validFrom
        };

        price.Update(values, source, utcNow);

        return price;
    }

    internal void Update(PriceValues values, PriceSource source, DateTime utcNow)
    {
        InputPerMillion = values.InputPerMillion;
        CachedInputPerMillion = values.CachedInputPerMillion;
        CacheWritePerMillion = values.CacheWritePerMillion;
        OutputPerMillion = values.OutputPerMillion;
        Source = source;
        RecordedOnUtc = utcNow;
    }

    private static decimal Cost(long tokens, decimal pricePerMillion) =>
        Math.Round(tokens * pricePerMillion / TokensPerUnit, CostDecimals);
}
