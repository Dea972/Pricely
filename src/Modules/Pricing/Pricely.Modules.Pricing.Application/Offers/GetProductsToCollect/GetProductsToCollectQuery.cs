using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductsToCollect;

/// <summary>
/// The products whose offers the periodic collection visits: every product of Catalog.
/// </summary>
public sealed record GetProductsToCollectQuery : IQuery<IReadOnlyCollection<Guid>>;
