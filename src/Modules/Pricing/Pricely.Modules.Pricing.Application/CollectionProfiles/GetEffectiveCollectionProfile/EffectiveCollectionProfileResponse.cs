namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetEffectiveCollectionProfile;

public sealed record EffectiveCollectionProfileResponse(
    IReadOnlyList<Guid> PreferredMerchantIds,
    Guid? PreferredMerchantsSourceCategoryId,
    string Instructions);
