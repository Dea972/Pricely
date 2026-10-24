namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// The identifiers of the reference product, used to evaluate a match.
/// </summary>
public sealed record ProductIdentity(string? Gtin, string Brand, string? ManufacturerReference);
