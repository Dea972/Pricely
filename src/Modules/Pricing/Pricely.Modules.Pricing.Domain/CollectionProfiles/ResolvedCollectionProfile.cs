namespace Pricely.Modules.Pricing.Domain.CollectionProfiles;

/// <summary>
/// The collection profile that actually applies to a product typology once inheritance is resolved.
/// </summary>
/// <param name="PreferredMerchantIds">Merchants to query, by priority.</param>
/// <param name="PreferredMerchantsSourceCategoryId">The typology whose preferred merchants are used, if any.</param>
/// <param name="Instructions">Instructions concatenated from the root typology down to the requested one.</param>
public sealed record ResolvedCollectionProfile(
    IReadOnlyList<Guid> PreferredMerchantIds,
    Guid? PreferredMerchantsSourceCategoryId,
    string Instructions)
{
    public bool HasPreferredMerchants => PreferredMerchantIds.Count > 0;
}
