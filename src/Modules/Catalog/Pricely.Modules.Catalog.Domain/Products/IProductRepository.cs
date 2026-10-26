namespace Pricely.Modules.Catalog.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithGtinAsync(Gtin gtin, Guid? excludedProductId, CancellationToken cancellationToken = default);

    Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default);

    void Insert(Product product);
}
