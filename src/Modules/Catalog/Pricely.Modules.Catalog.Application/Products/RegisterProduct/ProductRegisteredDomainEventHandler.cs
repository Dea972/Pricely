using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Application.Messaging;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.RegisterProduct;

internal sealed class ProductRegisteredDomainEventHandler(IProductRepository productRepository, IEventBus eventBus)
    : DomainEventHandler<ProductRegisteredDomainEvent>
{
    public override async Task Handle(
        ProductRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        Product product = await productRepository.GetAsync(domainEvent.ProductId, cancellationToken)
            ?? throw new PricelyException(
                nameof(ProductRegisteredDomainEventHandler),
                ProductErrors.NotFound(domainEvent.ProductId));

        await eventBus.PublishAsync(ProductIntegrationEventFactory.Registered(domainEvent, product), cancellationToken);
    }
}
