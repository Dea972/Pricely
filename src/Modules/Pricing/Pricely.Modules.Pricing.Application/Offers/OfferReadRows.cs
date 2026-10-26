namespace Pricely.Modules.Pricing.Application.Offers;

/// <summary>A flagged observation as Pricing stores it: the name of the product belongs to Catalog and is added by the caller.</summary>
public sealed class PriceAnomalyRow
{
    public Guid PriceObservationId { get; init; }

    public Guid OfferId { get; init; }

    public Guid ProductId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public string OfferStatus { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public DateTime ObservedOnUtc { get; init; }
}

/// <summary>An offer waiting for review as Pricing stores it: the identity of the product belongs to Catalog.</summary>
public sealed class PendingOfferRow
{
    public Guid OfferId { get; init; }

    public Guid ProductId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public string? MerchantProductReference { get; init; }

    public decimal? AiSimilarity { get; init; }

    public DateTime ProposedOnUtc { get; init; }

    public decimal? LatestAmount { get; init; }

    public string? LatestCurrency { get; init; }

    public string? ObservedTitle { get; init; }

    public string? ObservedGtin { get; init; }

    public string? ObservedBrand { get; init; }

    public string? ObservedManufacturerReference { get; init; }

    public string? AiRationale { get; init; }
}

/// <summary>An offer of a product with its latest price and the median of the confirmed offers (computed by the store).</summary>
public sealed class ProductOfferRow
{
    public Guid OfferId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? ConfirmationMethod { get; init; }

    public decimal? LatestAmount { get; init; }

    public string? LatestCurrency { get; init; }

    public DateTime? LatestObservedOnUtc { get; init; }

    public bool LatestIsFlaggedAsAnomaly { get; init; }

    public decimal? MedianAmount { get; init; }

    public string? MedianCurrency { get; init; }

    public decimal? DeviationFromMedian { get; init; }
}
