using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Categories;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetEffectiveCollectionProfile;

internal sealed class GetEffectiveCollectionProfileQueryHandler(
    IProductCategoryReader productCategoryReader,
    ICollectionProfileRepository collectionProfileRepository)
    : IQueryHandler<GetEffectiveCollectionProfileQuery, EffectiveCollectionProfileResponse>
{
    public async Task<Result<EffectiveCollectionProfileResponse>> Handle(
        GetEffectiveCollectionProfileQuery request,
        CancellationToken cancellationToken)
    {
        Result<ResolvedCollectionProfile> profile = await CollectionProfileLookup.ResolveAsync(
            request.ProductCategoryId,
            productCategoryReader,
            collectionProfileRepository,
            cancellationToken);

        if (profile.IsFailure)
        {
            return Result.Failure<EffectiveCollectionProfileResponse>(profile.Error);
        }

        ResolvedCollectionProfile resolved = profile.Value;

        return new EffectiveCollectionProfileResponse(
            resolved.PreferredMerchantIds,
            resolved.PreferredMerchantsSourceCategoryId,
            resolved.Instructions,
            [.. resolved.InstructionBlocks.Select(b => new InstructionBlockResponse(b.ProductCategoryId, b.Instructions))],
            resolved.OwnProfile is not null,
            [.. resolved.OwnProfile?.PreferredMerchants.Select(m => m.MerchantId) ?? []],
            resolved.OwnProfile?.Instructions);
    }
}
