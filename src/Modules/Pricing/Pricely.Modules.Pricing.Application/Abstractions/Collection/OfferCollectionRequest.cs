namespace Pricely.Modules.Pricing.Application.Abstractions.Collection;

/// <param name="Product">The reference product to look for.</param>
/// <param name="Merchants">Merchants to query, by priority (resolved from the typology's collection profile).</param>
/// <param name="Instructions">Collection instructions of the typology (prompt guidance), from general to specific.</param>
public sealed record OfferCollectionRequest(
    ProductToCollect Product,
    IReadOnlyList<MerchantToSearch> Merchants,
    string Instructions);

public sealed record ProductToCollect(
    Guid ProductId,
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference);

/// <param name="SearchTemplate">Search page of the merchant's site with <c>{query}</c>, if configured.</param>
public sealed record MerchantToSearch(Guid MerchantId, string Name, Uri Website, int Priority, string? SearchTemplate = null)
{
    /// <summary>Product page of the merchant's site reached by the GTIN (<c>{gtin13}</c> or <c>{gtin14}</c>), if configured.</summary>
    public string? ProductPageTemplate { get; init; }
}
