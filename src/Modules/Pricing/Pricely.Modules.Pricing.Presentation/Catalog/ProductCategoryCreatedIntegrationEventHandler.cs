using MediatR;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.IntegrationEvents;
using Pricely.Modules.Pricing.Application.Catalog.UpsertCatalogCategory;

namespace Pricely.Modules.Pricing.Presentation.Catalog;

internal sealed class ProductCategoryCreatedIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<ProductCategoryCreatedIntegrationEvent>
{
    public override async Task Handle(
        ProductCategoryCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new UpsertCatalogCategoryCommand(
                integrationEvent.ProductCategoryId,
                integrationEvent.Name,
                integrationEvent.ParentId),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(UpsertCatalogCategoryCommand), result.Error);
        }
    }
}
