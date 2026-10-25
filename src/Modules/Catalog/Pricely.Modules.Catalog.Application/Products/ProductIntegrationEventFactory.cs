using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.IntegrationEvents;

namespace Pricely.Modules.Catalog.Application.Products;

internal static class ProductIntegrationEventFactory
{
    internal static ProductRegisteredIntegrationEvent Registered(IDomainEvent domainEvent, Product product) =>
        new(domainEvent.Id, domainEvent.OccurredOnUtc, product.Id)
        {
            Name = product.Name,
            Brand = product.Brand,
            Gtin = product.Gtin?.Value,
            ManufacturerReference = product.ManufacturerReference,
            ProductCategoryId = product.ProductCategoryId
        };

    internal static ProductUpdatedIntegrationEvent Updated(IDomainEvent domainEvent, Product product) =>
        new(domainEvent.Id, domainEvent.OccurredOnUtc, product.Id)
        {
            Name = product.Name,
            Brand = product.Brand,
            Gtin = product.Gtin?.Value,
            ManufacturerReference = product.ManufacturerReference,
            ProductCategoryId = product.ProductCategoryId
        };
}
