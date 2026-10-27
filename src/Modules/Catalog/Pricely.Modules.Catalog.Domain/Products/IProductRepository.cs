namespace Pricely.Modules.Catalog.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithGtinAsync(Gtin gtin, Guid? excludedProductId, CancellationToken cancellationToken = default);

    Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default);

    /// <summary>The product of this GTIN, whatever its length (<see cref="Gtin.EquivalentForms"/>).</summary>
    Task<Product?> FindByEquivalentGtinAsync(Gtin gtin, CancellationToken cancellationToken = default);

    /// <summary>The products of this brand (case ignored), to compare their manufacturer reference.</summary>
    Task<IReadOnlyList<Product>> GetByBrandAsync(string brand, CancellationToken cancellationToken = default);

    void Insert(Product product);
}
