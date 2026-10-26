namespace Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;

public sealed record PendingOfferResponse
{
    public Guid OfferId { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; }

    public string ProductPage { get; init; }

    public string? MerchantProductReference { get; init; }

    public decimal? AiSimilarity { get; init; }

    public DateTime ProposedOnUtc { get; init; }

    public decimal? LatestAmount { get; init; }

    public string? LatestCurrency { get; init; }

    public string ProductBrand { get; init; }

    public string? ProductGtin { get; init; }

    public string? ProductManufacturerReference { get; init; }

    public string? ObservedTitle { get; init; }

    public string? ObservedGtin { get; init; }

    public string? ObservedBrand { get; init; }

    public string? ObservedManufacturerReference { get; init; }

    public string? AiRationale { get; init; }

    /// <summary>The page's identifiers against the reference (<c>OfferMatchingPolicy.Compare</c>): null when not shown.</summary>
    public bool? SameGtin { get; init; }

    public bool? SameBrand { get; init; }

    public bool? SameManufacturerReference { get; init; }
}
