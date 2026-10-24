using MediatR;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.IntegrationEvents;
using Pricely.Modules.Pricing.Application.Catalog.RenameCatalogCategory;

namespace Pricely.Modules.Pricing.Presentation.Catalog;

internal sealed class ProductCategoryRenamedIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<ProductCategoryRenamedIntegrationEvent>
{
    public override async Task Handle(
        ProductCategoryRenamedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RenameCatalogCategoryCommand(integrationEvent.ProductCategoryId, integrationEvent.Name),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(RenameCatalogCategoryCommand), result.Error);
        }
    }
}
