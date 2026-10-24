namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// What the collector observed on the merchant page to link it to the reference product.
/// </summary>
/// <param name="ObservedGtin">GTIN / EAN displayed by the merchant, if any.</param>
/// <param name="ObservedBrand">Brand displayed by the merchant, if any.</param>
/// <param name="ObservedManufacturerReference">Manufacturer reference displayed by the merchant, if any.</param>
/// <param name="AiSimilarity">Similarity score between 0 and 1 given by the collector, if any.</param>
public sealed record MatchEvidence(
    string? ObservedGtin,
    string? ObservedBrand,
    string? ObservedManufacturerReference,
    decimal? AiSimilarity);
