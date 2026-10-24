namespace Pricely.Modules.Pricing.Domain.Catalog;

public sealed record CatalogProductSnapshot(
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid ProductCategoryId);
