using Pricely.Modules.Catalog.Application.Products.GetProduct;

namespace Pricely.Modules.Catalog.Application.Products;

/// <summary>
/// Read side of the products (queries): the data access is behind this port, implemented in Infrastructure.
/// </summary>
public interface IProductReadStore
{
    Task<ProductResponse?> GetAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Products whose name, brand or manufacturer reference contains the text, or whose GTIN equals it, by name.</summary>
    Task<IReadOnlyCollection<ProductResponse>> SearchAsync(
        string? text,
        Guid? productCategoryId,
        int limit,
        CancellationToken cancellationToken = default);
}
