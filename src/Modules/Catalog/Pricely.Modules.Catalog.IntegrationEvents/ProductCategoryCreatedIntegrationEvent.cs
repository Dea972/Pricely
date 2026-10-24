using Pricely.Common.Application.EventBus;

namespace Pricely.Modules.Catalog.IntegrationEvents;

public sealed class ProductCategoryCreatedIntegrationEvent : IntegrationEvent
{
    public ProductCategoryCreatedIntegrationEvent(
        Guid id,
        DateTime occurredOnUtc,
        Guid productCategoryId,
        string name,
        Guid? parentId)
        : base(id, occurredOnUtc)
    {
        ProductCategoryId = productCategoryId;
        Name = name;
        ParentId = parentId;
    }

    public Guid ProductCategoryId { get; init; }

    public string Name { get; init; }

    public Guid? ParentId { get; init; }
}
