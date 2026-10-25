using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Application.Messaging;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.IntegrationEvents;

namespace Pricely.Modules.Catalog.Application.Categories.CreateProductCategory;

internal sealed class ProductCategoryCreatedDomainEventHandler(
    IProductCategoryRepository productCategoryRepository,
    IEventBus eventBus)
    : DomainEventHandler<ProductCategoryCreatedDomainEvent>
{
    public override async Task Handle(
        ProductCategoryCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ProductCategory category = await productCategoryRepository.GetAsync(domainEvent.ProductCategoryId, cancellationToken)
            ?? throw new PricelyException(
                nameof(ProductCategoryCreatedDomainEventHandler),
                ProductCategoryErrors.NotFound(domainEvent.ProductCategoryId));

        await eventBus.PublishAsync(
            new ProductCategoryCreatedIntegrationEvent(
                domainEvent.Id,
                domainEvent.OccurredOnUtc,
                category.Id,
                category.Name,
                category.ParentId),
            cancellationToken);
    }
}
