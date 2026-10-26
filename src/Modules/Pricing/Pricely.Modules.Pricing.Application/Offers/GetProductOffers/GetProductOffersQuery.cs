using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductOffers;

/// <summary>
/// Every offer of a product that is not rejected (confirmed and proposed), with how it was matched, its latest price
/// and its deviation from the median of the confirmed offers.
/// </summary>
public sealed record GetProductOffersQuery(Guid ProductId) : IQuery<ProductOffersResponse>;
