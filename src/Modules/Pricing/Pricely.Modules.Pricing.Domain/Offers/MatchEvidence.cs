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
    decimal? AiSimilarity)
{
    public const int ObservedGtinMaxLength = 14;

    public const int ObservedBrandMaxLength = 150;

    public const int ObservedManufacturerReferenceMaxLength = 100;

    public const int ObservedTitleMaxLength = 300;

    public const int AiRationaleMaxLength = 1000;

    /// <summary>Title of the product on the merchant page, as displayed.</summary>
    public string? ObservedTitle { get; init; }

    /// <summary>Why the collector thinks it is (or not) the same product, shown to the reviewer.</summary>
    public string? AiRationale { get; init; }
}
