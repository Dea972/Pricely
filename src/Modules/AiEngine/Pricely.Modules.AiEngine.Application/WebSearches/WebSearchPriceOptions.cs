namespace Pricely.Modules.AiEngine.Application.WebSearches;

/// <summary>
/// Price of one web search per provider, in USD (section <c>AiEngine:WebSearch</c>, ADR-027). Anthropic bills $10 per
/// 1,000 searches; a provider without a price is journaled unpriced.
/// </summary>
public sealed class WebSearchPriceOptions
{
    public const string SectionName = "AiEngine:WebSearch";

    public Dictionary<string, decimal> PricePerSearch { get; init; } = new(StringComparer.OrdinalIgnoreCase) { ["Anthropic"] = 0.01m };
}
