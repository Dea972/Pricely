namespace Pricely.Modules.Catalog.Domain.Categories;

public interface IProductCategoryRepository
{
    Task<ProductCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    void Insert(ProductCategory category);
}
