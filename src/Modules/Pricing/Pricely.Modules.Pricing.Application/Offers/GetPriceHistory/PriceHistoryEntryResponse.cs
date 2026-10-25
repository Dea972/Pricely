namespace Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;

public sealed class PriceHistoryEntryResponse
{
    public Guid OfferId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; }

    public string Availability { get; init; }

    public DateTime ObservedOnUtc { get; init; }

    public bool IsFlaggedAsAnomaly { get; init; }
}
