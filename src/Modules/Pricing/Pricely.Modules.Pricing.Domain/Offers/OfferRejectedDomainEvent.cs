using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

public sealed class OfferRejectedDomainEvent(Guid offerId, Guid productId, Guid merchantId) : DomainEvent
{
    public Guid OfferId { get; init; } = offerId;

    public Guid ProductId { get; init; } = productId;

    public Guid MerchantId { get; init; } = merchantId;
}
