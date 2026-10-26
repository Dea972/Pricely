using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles;

/// <summary>
/// Reads a typology's lineage from Catalog and the profiles defined along it, then resolves the effective profile.
/// </summary>
internal static class CollectionProfileLookup
{
    internal static async Task<Result<ResolvedCollectionProfile>> ResolveAsync(
        Guid productCategoryId,
        IProductCategoryReader productCategoryReader,
        ICollectionProfileRepository collectionProfileRepository,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductCategorySummary>> lineage =
            await productCategoryReader.GetLineageAsync(productCategoryId, cancellationToken);
        if (lineage.IsFailure)
        {
            return Result.Failure<ResolvedCollectionProfile>(lineage.Error);
        }

        if (lineage.Value.Count == 0)
        {
            return Result.Failure<ResolvedCollectionProfile>(CatalogErrors.CategoryNotFound(productCategoryId));
        }

        // The typology followed by its known ancestors, nearest first.
        List<Guid> categoryIds = [.. lineage.Value.Select(c => c.Id)];
        IReadOnlyCollection<CollectionProfile> profiles =
            await collectionProfileRepository.GetByCategoriesAsync(categoryIds, cancellationToken);

        return CollectionProfileResolver.Resolve(categoryIds, profiles);
    }
}
