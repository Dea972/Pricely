using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Application.Messaging;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.UpdateProduct;

internal sealed class ProductUpdatedDomainEventHandler(IProductRepository productRepository, IEventBus eventBus)
    : DomainEventHandler<ProductUpdatedDomainEvent>
{
    public override async Task Handle(
        ProductUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        Product product = await productRepository.GetAsync(domainEvent.ProductId, cancellationToken)
            ?? throw new PricelyException(
                nameof(ProductUpdatedDomainEventHandler),
                ProductErrors.NotFound(domainEvent.ProductId));

        await eventBus.PublishAsync(ProductIntegrationEventFactory.Updated(domainEvent, product), cancellationToken);
    }
}
