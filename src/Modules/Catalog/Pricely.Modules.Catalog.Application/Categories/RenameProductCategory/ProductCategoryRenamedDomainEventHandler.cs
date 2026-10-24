using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Messaging;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.IntegrationEvents;

namespace Pricely.Modules.Catalog.Application.Categories.RenameProductCategory;

internal sealed class ProductCategoryRenamedDomainEventHandler(IEventBus eventBus)
    : DomainEventHandler<ProductCategoryRenamedDomainEvent>
{
    public override Task Handle(
        ProductCategoryRenamedDomainEvent domainEvent,
        CancellationToken cancellationToken = default) =>
        eventBus.PublishAsync(
            new ProductCategoryRenamedIntegrationEvent(
                domainEvent.Id,
                domainEvent.OccurredOnUtc,
                domainEvent.ProductCategoryId,
                domainEvent.Name),
            cancellationToken);
}
