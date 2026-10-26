namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetEffectiveCollectionProfile;

/// <param name="PreferredMerchantIds">Merchants queried, by priority (own or inherited).</param>
/// <param name="PreferredMerchantsSourceCategoryId">The typology they come from; another one than the requested: inherited.</param>
/// <param name="Instructions">The collection prompt of the typology: <see cref="InstructionBlocks"/> joined.</param>
/// <param name="InstructionBlocks">What each typology adds, from the root down to the requested one.</param>
/// <param name="HasOwnProfile">The typology defines its own profile.</param>
/// <param name="OwnPreferredMerchantIds">The merchants of its own profile (empty: inherited).</param>
/// <param name="OwnInstructions">The instructions of its own profile.</param>
public sealed record EffectiveCollectionProfileResponse(
    IReadOnlyList<Guid> PreferredMerchantIds,
    Guid? PreferredMerchantsSourceCategoryId,
    string Instructions,
    IReadOnlyList<InstructionBlockResponse> InstructionBlocks,
    bool HasOwnProfile,
    IReadOnlyList<Guid> OwnPreferredMerchantIds,
    string? OwnInstructions);

public sealed record InstructionBlockResponse(Guid ProductCategoryId, string Instructions);
