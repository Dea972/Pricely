using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.DefineCollectionProfile;

/// <param name="ProductCategoryId">The typology the profile applies to.</param>
/// <param name="PreferredMerchantIds">Merchants to query first, by priority. Empty: inherit from the parent typology.</param>
/// <param name="Instructions">Collection instructions specific to this typology. Added after the parent's instructions.</param>
public sealed record DefineCollectionProfileCommand(
    Guid ProductCategoryId,
    IReadOnlyList<Guid> PreferredMerchantIds,
    string? Instructions) : ICommand<Guid>;
