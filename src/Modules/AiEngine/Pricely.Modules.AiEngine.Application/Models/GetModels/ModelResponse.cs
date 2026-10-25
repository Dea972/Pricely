namespace Pricely.Modules.AiEngine.Application.Models.GetModels;

public sealed class ModelResponse
{
    public Guid Id { get; init; }

    public string Provider { get; init; }

    public string ProviderModelId { get; init; }

    public string Label { get; init; }

    /// <summary>First day of the current price; null when the model has no price yet.</summary>
    public DateTime? PriceValidFrom { get; init; }

    public decimal? InputPerMillion { get; init; }

    public decimal? CachedInputPerMillion { get; init; }

    public decimal? CacheWritePerMillion { get; init; }

    public decimal? OutputPerMillion { get; init; }
}
