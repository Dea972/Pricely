namespace Pricely.Modules.Pricing.Application.CollectionProfiles;

/// <summary>
/// Read side of the collection profiles (queries), implemented in Infrastructure.
/// </summary>
public interface ICollectionProfileReadStore
{
    /// <summary>One row per defined profile; the name of the typology belongs to Catalog and is added by the caller.</summary>
    Task<IReadOnlyCollection<CollectionProfileRow>> GetAllAsync(CancellationToken cancellationToken = default);
}

public sealed record CollectionProfileRow(Guid ProductCategoryId, int PreferredMerchantCount, bool HasInstructions);
