using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;
using Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;

namespace Pricely.Modules.Pricing.Application.Offers;

/// <summary>
/// Read side of the offers and their price observations (queries), implemented in Infrastructure. Pricing stores the
/// identifier of the product only: the rows carry it, and the handlers read the product from Catalog (contract).
/// </summary>
public interface IOfferReadStore
{
    Task<IReadOnlyCollection<PriceAnomalyRow>> GetPriceAnomaliesAsync(DateTime? sinceUtc, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PriceHistoryEntryResponse>> GetPriceHistoryAsync(
        Guid productId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>The latest price of each confirmed offer of an active merchant, cheapest first.</summary>
    Task<IReadOnlyCollection<MerchantPriceResponse>> GetPriceComparisonAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PendingOfferRow>> GetPendingReviewAsync(CancellationToken cancellationToken = default);

    /// <summary>The offers of a product that are not rejected, confirmed first, with the median of the confirmed ones.</summary>
    Task<IReadOnlyCollection<ProductOfferRow>> GetProductOffersAsync(Guid productId, CancellationToken cancellationToken = default);
}
