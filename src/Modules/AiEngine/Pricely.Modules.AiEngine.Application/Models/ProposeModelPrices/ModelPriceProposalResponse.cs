namespace Pricely.Modules.AiEngine.Application.Models.ProposeModelPrices;

/// <param name="Current">The price in force today, if any.</param>
/// <param name="Proposed">The price found, if any.</param>
/// <param name="Source"><c>LiteLlm</c> or <c>AiReading</c>, where <see cref="Proposed"/> comes from.</param>
/// <param name="IsChanged">The proposal differs from the price in force (or there is none).</param>
/// <param name="Notes">
/// <c>NotFound</c> (no source knows the model), <c>PriceListUnavailable</c> (LiteLLM could not be read),
/// <c>CachePricesMissing</c> (the source has no cache price: the input price is proposed), <c>ToCheck</c> (read by the AI).
/// </param>
public sealed record ModelPriceProposalResponse(
    Guid ModelId,
    string Provider,
    string ProviderModelId,
    PriceResponse? Current,
    PriceResponse? Proposed,
    string? Source,
    bool IsChanged,
    IReadOnlyList<string> Notes);

/// <summary>USD per million tokens.</summary>
public sealed record PriceResponse(
    decimal InputPerMillion,
    decimal CachedInputPerMillion,
    decimal CacheWritePerMillion,
    decimal OutputPerMillion,
    DateOnly? ValidFrom = null,
    string? Source = null);
