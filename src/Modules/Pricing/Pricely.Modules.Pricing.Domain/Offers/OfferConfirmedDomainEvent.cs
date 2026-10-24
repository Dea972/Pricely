using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

public sealed class OfferConfirmedDomainEvent(
    Guid offerId,
    Guid productId,
    Guid merchantId,
    OfferConfirmationMethod confirmationMethod) : DomainEvent
{
    public Guid OfferId { get; init; } = offerId;

    public Guid ProductId { get; init; } = productId;

    public Guid MerchantId { get; init; } = merchantId;

    public OfferConfirmationMethod ConfirmationMethod { get; init; } = confirmationMethod;
}
