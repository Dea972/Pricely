using Pricely.Modules.Catalog.Application.Categories.GetProductCategories;

namespace Pricely.Modules.Catalog.Application.Categories;

/// <summary>
/// Read side of the product categories (queries), implemented in Infrastructure.
/// </summary>
public interface IProductCategoryReadStore
{
    /// <summary>Every typology, roots first, then by name.</summary>
    Task<IReadOnlyCollection<ProductCategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
