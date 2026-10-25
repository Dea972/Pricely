namespace Pricely.Modules.Catalog.Application.Products.GetProduct;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid ProductCategoryId);
