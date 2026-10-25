namespace Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;

public sealed class PriceAnomalyResponse
{
    public Guid PriceObservationId { get; init; }

    public Guid OfferId { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; }

    public string MerchantName { get; init; }

    public string ProductPage { get; init; }

    public string OfferStatus { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; }

    public DateTime ObservedOnUtc { get; init; }
}
