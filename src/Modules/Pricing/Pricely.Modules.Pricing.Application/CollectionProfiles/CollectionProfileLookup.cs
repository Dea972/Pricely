using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles;

/// <summary>
/// Loads a typology's lineage and the profiles defined along it, then resolves the effective profile.
/// </summary>
internal static class CollectionProfileLookup
{
    /// <summary>
    /// Guard against corrupted hierarchies (Catalog limits the tree to 3 levels).
    /// </summary>
    private const int MaxLineageLength = 10;

    internal static async Task<Result<ResolvedCollectionProfile>> ResolveAsync(
        Guid productCategoryId,
        ICatalogCategoryRepository catalogCategoryRepository,
        ICollectionProfileRepository collectionProfileRepository,
        CancellationToken cancellationToken)
    {
        List<Guid> lineage = await GetLineageAsync(productCategoryId, catalogCategoryRepository, cancellationToken);
        if (lineage.Count == 0)
        {
            return Result.Failure<ResolvedCollectionProfile>(CatalogErrors.CategoryNotFound(productCategoryId));
        }

        IReadOnlyCollection<CollectionProfile> profiles =
            await collectionProfileRepository.GetByCategoriesAsync(lineage, cancellationToken);

        return CollectionProfileResolver.Resolve(lineage, profiles);
    }

    /// <summary>
    /// The typology followed by its known ancestors, nearest first.
    /// </summary>
    private static async Task<List<Guid>> GetLineageAsync(
        Guid productCategoryId,
        ICatalogCategoryRepository catalogCategoryRepository,
        CancellationToken cancellationToken)
    {
        List<Guid> lineage = [];
        Guid? currentCategoryId = productCategoryId;

        while (currentCategoryId is not null && lineage.Count < MaxLineageLength)
        {
            CatalogCategory? category = await catalogCategoryRepository.GetAsync(currentCategoryId.Value, cancellationToken);
            if (category is null || lineage.Contains(category.Id))
            {
                break;
            }

            lineage.Add(category.Id);
            currentCategoryId = category.ParentId;
        }

        return lineage;
    }
}
