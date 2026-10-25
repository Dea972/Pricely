using MediatR;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.IntegrationEvents;
using Pricely.Modules.Pricing.Application.Catalog.UpsertCatalogProduct;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Presentation.Catalog;

internal sealed class ProductRegisteredIntegrationEventHandler(ISender sender)
    : IntegrationEventHandler<ProductRegisteredIntegrationEvent>
{
    public override async Task Handle(
        ProductRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new UpsertCatalogProductCommand(
                integrationEvent.ProductId,
                new CatalogProductSnapshot(
                    integrationEvent.Name,
                    integrationEvent.Brand,
                    integrationEvent.Gtin,
                    integrationEvent.ManufacturerReference,
                    integrationEvent.ProductCategoryId)),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(UpsertCatalogProductCommand), result.Error);
        }
    }
}
