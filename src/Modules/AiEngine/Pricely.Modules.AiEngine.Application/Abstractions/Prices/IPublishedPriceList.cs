using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.Abstractions.Prices;

/// <summary>
/// Port to a public price list of the models (LiteLLM, ADR-021). Implemented in Infrastructure.
/// </summary>
public interface IPublishedPriceList
{
    /// <summary>The published prices of these models; a model the list does not know is simply absent.</summary>
    Task<Result<IReadOnlyCollection<PublishedPrice>>> FindAsync(
        IReadOnlyCollection<PublishedPriceQuery> models,
        CancellationToken cancellationToken = default);
}

public sealed record PublishedPriceQuery(Provider Provider, string ProviderModelId);

/// <summary>USD per million tokens; the cache prices are null when the list does not publish them.</summary>
public sealed record PublishedPrice(
    Provider Provider,
    string ProviderModelId,
    decimal InputPerMillion,
    decimal? CachedInputPerMillion,
    decimal? CacheWritePerMillion,
    decimal OutputPerMillion);

/// <summary>Where the prices are published (section <c>AiEngine:Prices</c>).</summary>
public sealed class ModelPriceOptions
{
    public const string SectionName = "AiEngine:Prices";

    /// <summary>The price list of LiteLLM (<c>model_prices_and_context_window.json</c>); none: the AI alone.</summary>
    public Uri? LiteLlmPriceList { get; set; }

    /// <summary>The pricing page of each provider (by provider name), read by the AI for a model LiteLLM does not know.</summary>
    public Dictionary<string, Uri> PricingPages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
