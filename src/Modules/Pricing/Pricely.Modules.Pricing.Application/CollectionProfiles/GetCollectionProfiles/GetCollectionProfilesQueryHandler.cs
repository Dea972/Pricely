using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetCollectionProfiles;

/// <summary>The profiles come from Pricing, the names of the typologies from Catalog (contract).</summary>
internal sealed class GetCollectionProfilesQueryHandler(
    ICollectionProfileReadStore readStore,
    IProductCategoryReader productCategoryReader)
    : IQueryHandler<GetCollectionProfilesQuery, IReadOnlyCollection<CollectionProfileSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<CollectionProfileSummaryResponse>>> Handle(
        GetCollectionProfilesQuery request,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductCategorySummary>> categories = await productCategoryReader.GetAllAsync(cancellationToken);
        if (categories.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<CollectionProfileSummaryResponse>>(categories.Error);
        }

        var names = categories.Value.ToDictionary(c => c.Id, c => c.Name);
        IReadOnlyCollection<CollectionProfileRow> profiles = await readStore.GetAllAsync(cancellationToken);

        // A profile whose typology Catalog no longer knows has no name and comes last.
        IReadOnlyCollection<CollectionProfileSummaryResponse> summaries =
        [
            .. profiles
                .Select(p => new CollectionProfileSummaryResponse(
                    p.ProductCategoryId,
                    names.GetValueOrDefault(p.ProductCategoryId),
                    p.PreferredMerchantCount,
                    p.HasInstructions))
                .OrderBy(s => s.CategoryName is null)
                .ThenBy(s => s.CategoryName, StringComparer.OrdinalIgnoreCase)
        ];

        return Result.Success(summaries);
    }
}
