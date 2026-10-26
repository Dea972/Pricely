namespace Pricely.Modules.Pricing.Application.Offers.GetProductOffers;

public sealed record ProductOffersResponse(
    Guid ProductId,
    decimal? ConfirmedMedianAmount,
    string? Currency,
    IReadOnlyCollection<ProductOfferResponse> Offers);

public sealed class ProductOfferResponse
{
    public Guid OfferId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    /// <summary><c>Confirmed</c> or <c>Proposed</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary><c>Gtin</c>, <c>ManufacturerReference</c> or <c>Manual</c> for a confirmed offer; null while proposed.</summary>
    public string? ConfirmationMethod { get; init; }

    public decimal? LatestAmount { get; init; }

    public string? LatestCurrency { get; init; }

    public DateTime? LatestObservedOnUtc { get; init; }

    public bool LatestIsFlaggedAsAnomaly { get; init; }

    /// <summary>(latest price − median of confirmed offers) / median, same currency only; null when not computable.</summary>
    public decimal? DeviationFromMedian { get; init; }
}
