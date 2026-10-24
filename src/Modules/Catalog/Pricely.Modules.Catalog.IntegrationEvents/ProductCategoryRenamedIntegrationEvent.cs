using Pricely.Common.Application.EventBus;

namespace Pricely.Modules.Catalog.IntegrationEvents;

public sealed class ProductCategoryRenamedIntegrationEvent : IntegrationEvent
{
    public ProductCategoryRenamedIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid productCategoryId, string name)
        : base(id, occurredOnUtc)
    {
        ProductCategoryId = productCategoryId;
        Name = name;
    }

    public Guid ProductCategoryId { get; init; }

    public string Name { get; init; }
}
