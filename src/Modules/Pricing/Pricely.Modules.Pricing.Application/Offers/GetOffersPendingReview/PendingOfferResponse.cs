namespace Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;

public sealed class PendingOfferResponse
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
}
