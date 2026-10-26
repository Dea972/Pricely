namespace Pricely.Modules.Catalog.Application.Products.SuggestProduct;

/// <param name="Gtin">Kept only if valid and shown by the page.</param>
/// <param name="ManufacturerReference">Kept only if shown by the page.</param>
/// <param name="ProductCategoryId">A typology of the catalogue, if the model found one.</param>
/// <param name="ExistingProductId">A product of the catalogue with the same GTIN: no second one.</param>
/// <param name="IsAiAnswer">False when the model could not answer: only the title of the page is proposed.</param>
public sealed record ProductSuggestionResponse(
    Uri ProductPage,
    string? Name,
    string? Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid? ProductCategoryId,
    string? Packaging,
    string? Rationale,
    Guid? ExistingProductId,
    string? ExistingProductName,
    bool IsAiAnswer);
