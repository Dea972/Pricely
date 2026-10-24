using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

public sealed class PriceAnomalyDetectedDomainEvent(Guid priceObservationId, Guid offerId, Guid productId) : DomainEvent
{
    public Guid PriceObservationId { get; init; } = priceObservationId;

    public Guid OfferId { get; init; } = offerId;

    public Guid ProductId { get; init; } = productId;
}
