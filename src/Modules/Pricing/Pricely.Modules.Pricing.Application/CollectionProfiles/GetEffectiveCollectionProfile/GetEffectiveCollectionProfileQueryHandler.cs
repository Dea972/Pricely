using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetEffectiveCollectionProfile;

internal sealed class GetEffectiveCollectionProfileQueryHandler(
    ICatalogCategoryRepository catalogCategoryRepository,
    ICollectionProfileRepository collectionProfileRepository)
    : IQueryHandler<GetEffectiveCollectionProfileQuery, EffectiveCollectionProfileResponse>
{
    public async Task<Result<EffectiveCollectionProfileResponse>> Handle(
        GetEffectiveCollectionProfileQuery request,
        CancellationToken cancellationToken)
    {
        Result<ResolvedCollectionProfile> profile = await CollectionProfileLookup.ResolveAsync(
            request.ProductCategoryId,
            catalogCategoryRepository,
            collectionProfileRepository,
            cancellationToken);

        if (profile.IsFailure)
        {
            return Result.Failure<EffectiveCollectionProfileResponse>(profile.Error);
        }

        return new EffectiveCollectionProfileResponse(
            profile.Value.PreferredMerchantIds,
            profile.Value.PreferredMerchantsSourceCategoryId,
            profile.Value.Instructions);
    }
}
