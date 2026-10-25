using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Categories;

public sealed class ProductCategoryRenamedDomainEvent(Guid productCategoryId, string name) : DomainEvent
{
    public Guid ProductCategoryId { get; init; } = productCategoryId;

    public string Name { get; init; } = name;
}
