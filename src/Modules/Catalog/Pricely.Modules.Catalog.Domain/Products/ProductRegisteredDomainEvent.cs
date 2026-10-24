using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Products;

public sealed class ProductRegisteredDomainEvent(Guid productId) : DomainEvent
{
    public Guid ProductId { get; init; } = productId;
}
