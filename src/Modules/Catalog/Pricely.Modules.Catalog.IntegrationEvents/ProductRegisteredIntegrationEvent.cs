using Pricely.Common.Application.EventBus;

namespace Pricely.Modules.Catalog.IntegrationEvents;

public sealed class ProductRegisteredIntegrationEvent : IntegrationEvent
{
    public ProductRegisteredIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid productId)
        : base(id, occurredOnUtc)
    {
        ProductId = productId;
    }

    public Guid ProductId { get; init; }

    public required string Name { get; init; }

    public required string Brand { get; init; }

    public string? Gtin { get; init; }

    public string? ManufacturerReference { get; init; }

    public required Guid ProductCategoryId { get; init; }
}
