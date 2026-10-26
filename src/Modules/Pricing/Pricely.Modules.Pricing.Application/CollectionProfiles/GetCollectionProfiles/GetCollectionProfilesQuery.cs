using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.GetCollectionProfiles;

/// <summary>The typologies that define their own collection profile (the others inherit).</summary>
public sealed record GetCollectionProfilesQuery : IQuery<IReadOnlyCollection<CollectionProfileSummaryResponse>>;
