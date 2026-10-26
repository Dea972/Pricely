namespace Pricely.Modules.Catalog.Application.Products.SuggestProduct;

/// <summary>The JSON answer of the model of <see cref="CatalogAiFeatures.ProductEntry"/>.</summary>
internal sealed record ProductEntryReply(
    string? Name,
    string? Brand,
    string? Gtin,
    string? ManufacturerReference,
    string? CategoryId,
    string? Packaging,
    string? Rationale);
