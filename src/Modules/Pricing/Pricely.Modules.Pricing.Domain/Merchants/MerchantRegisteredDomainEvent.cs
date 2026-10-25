using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Merchants;

public sealed class MerchantRegisteredDomainEvent(Guid merchantId) : DomainEvent
{
    public Guid MerchantId { get; init; } = merchantId;
}
