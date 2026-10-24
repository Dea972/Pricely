namespace Pricely.Modules.Pricing.Domain.CollectionProfiles;

/// <summary>
/// Resolves the collection profile of a typology from its own profile and its ancestors' profiles.
/// <list type="bullet">
/// <item>Preferred merchants: the nearest typology that defines some wins (override).</item>
/// <item>Instructions: concatenated from the root down to the typology (the parent frames, the child refines).</item>
/// </list>
/// </summary>
public static class CollectionProfileResolver
{
    public const string InstructionsSeparator = "\n\n";

    /// <param name="categoryLineage">The typology followed by its ancestors, nearest first (leaf, parent, root).</param>
    /// <param name="profiles">The profiles defined for any typology of the lineage.</param>
    public static ResolvedCollectionProfile Resolve(
        IReadOnlyList<Guid> categoryLineage,
        IReadOnlyCollection<CollectionProfile> profiles)
    {
        var profilesByCategory = profiles.ToDictionary(p => p.ProductCategoryId);

        IReadOnlyList<Guid> preferredMerchantIds = [];
        Guid? sourceCategoryId = null;

        foreach (Guid categoryId in categoryLineage)
        {
            if (profilesByCategory.TryGetValue(categoryId, out CollectionProfile? profile) &&
                profile.PreferredMerchants.Count > 0)
            {
                preferredMerchantIds = [.. profile.PreferredMerchants.Select(m => m.MerchantId)];
                sourceCategoryId = categoryId;
                break;
            }
        }

        IEnumerable<string> instructionsFromRoot = categoryLineage
            .Reverse()
            .Select(categoryId => profilesByCategory.GetValueOrDefault(categoryId)?.Instructions)
            .OfType<string>();

        return new ResolvedCollectionProfile(
            preferredMerchantIds,
            sourceCategoryId,
            string.Join(InstructionsSeparator, instructionsFromRoot));
    }
}
