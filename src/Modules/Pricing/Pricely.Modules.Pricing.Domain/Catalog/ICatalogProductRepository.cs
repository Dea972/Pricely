namespace Pricely.Modules.Pricing.Domain.Catalog;

public interface ICatalogProductRepository
{
    Task<CatalogProduct?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetAllIdsAsync(CancellationToken cancellationToken = default);

    void Insert(CatalogProduct product);
}
