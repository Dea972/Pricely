namespace Pricely.Modules.Pricing.Domain.CollectionProfiles;

public interface ICollectionProfileRepository
{
    Task<CollectionProfile?> GetByCategoryAsync(Guid productCategoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CollectionProfile>> GetByCategoriesAsync(
        IReadOnlyCollection<Guid> productCategoryIds,
        CancellationToken cancellationToken = default);

    void Insert(CollectionProfile profile);
}
