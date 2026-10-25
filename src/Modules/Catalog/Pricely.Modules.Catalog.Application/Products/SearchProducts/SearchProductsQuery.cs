using Pricely.Common.Application.Messaging;
using Pricely.Modules.Catalog.Application.Products.GetProduct;

namespace Pricely.Modules.Catalog.Application.Products.SearchProducts;

/// <summary>
/// Products whose name, brand or manufacturer reference contains <see cref="Text"/>, or whose GTIN equals it,
/// optionally restricted to a category (not its subcategories).
/// </summary>
public sealed record SearchProductsQuery(string? Text, Guid? ProductCategoryId, int Limit)
    : IQuery<IReadOnlyCollection<ProductResponse>>
{
    public const int MaxLimit = 50;
}
