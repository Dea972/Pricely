namespace Pricely.Modules.Catalog.Contracts.Products;

/// <summary>
/// A reference product as Catalog exposes it to the other modules.
/// </summary>
/// <param name="Id">Identifier of the product (the key other modules keep in a plain column, without foreign key).</param>
/// <param name="Name">Name of the product.</param>
/// <param name="Brand">Brand of the product.</param>
/// <param name="Gtin">GTIN (14 digits); null when the product has none.</param>
/// <param name="ManufacturerReference">Manufacturer reference; null when unknown.</param>
/// <param name="ProductCategoryId">Identifier of the typology of the product.</param>
public sealed record ProductSummary(
    Guid Id,
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid ProductCategoryId);
