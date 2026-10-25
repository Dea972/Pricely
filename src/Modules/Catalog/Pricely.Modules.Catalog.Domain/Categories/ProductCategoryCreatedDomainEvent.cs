using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Categories;

public sealed class ProductCategoryCreatedDomainEvent(Guid productCategoryId) : DomainEvent
{
    public Guid ProductCategoryId { get; init; } = productCategoryId;
}
