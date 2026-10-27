using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Products.FindOrRegisterProduct;

/// <summary>
/// The product of this GTIN (or of this brand and manufacturer reference) if Catalog knows it, else a new product in the
/// typology named by <see cref="CategoryPath"/>, whose missing levels are created (ADR-028). Identified by natural keys,
/// so that it can follow other actions of the same batch.
/// </summary>
/// <param name="CategoryPath">Names of the typology, from the root (1 to 3 levels), for a new product only.</param>
public sealed record FindOrRegisterProductCommand(
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference,
    IReadOnlyList<string> CategoryPath) : ICommand<FoundProductResponse>;

/// <param name="IsNew">Whether the product was registered by this command (false: it already existed).</param>
/// <param name="CreatedCategories">The typologies created for it, from the root.</param>
public sealed record FoundProductResponse(
    Guid ProductId,
    string Name,
    string Brand,
    string? Gtin,
    Guid ProductCategoryId,
    bool IsNew,
    IReadOnlyList<string> CreatedCategories);
