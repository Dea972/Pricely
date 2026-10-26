namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetCollectionProfiles;

/// <param name="PreferredMerchantCount">0: the typology inherits the merchants of its parent.</param>
public sealed record CollectionProfileSummaryResponse(
    Guid ProductCategoryId,
    string? CategoryName,
    int PreferredMerchantCount,
    bool HasInstructions);
