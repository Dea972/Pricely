namespace Pricely.Modules.Catalog.Domain.Products;

/// <summary>
/// Descriptive data of a product, validated by <see cref="Product"/>.
/// </summary>
public sealed record ProductDescription(string Name, string Brand, Gtin? Gtin, string? ManufacturerReference);
