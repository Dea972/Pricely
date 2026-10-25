using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetEffectiveCollectionProfile;

/// <summary>
/// The profile that actually applies to a typology, once inherited merchants and instructions are resolved.
/// </summary>
public sealed record GetEffectiveCollectionProfileQuery(Guid ProductCategoryId) : IQuery<EffectiveCollectionProfileResponse>;
