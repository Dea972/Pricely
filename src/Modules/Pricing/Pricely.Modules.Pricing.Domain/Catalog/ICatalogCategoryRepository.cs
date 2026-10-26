namespace Pricely.Modules.Pricing.Domain.Catalog;

public interface ICatalogCategoryRepository
{
    Task<CatalogCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    void Insert(CatalogCategory category);
}
