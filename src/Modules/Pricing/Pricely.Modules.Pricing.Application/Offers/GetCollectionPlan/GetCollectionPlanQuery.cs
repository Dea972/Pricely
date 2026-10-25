using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;

/// <summary>
/// What a collection of the product would query: the reference product, the active preferred merchants of its
/// typology (inherited if needed) by priority, and the typology's collection instructions.
/// </summary>
public sealed record GetCollectionPlanQuery(Guid ProductId) : IQuery<CollectionPlanResponse>;
