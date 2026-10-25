using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;

/// <summary>
/// The confirmed offers of a product with their latest price, cheapest first.
/// </summary>
public sealed record GetProductPriceComparisonQuery(Guid ProductId) : IQuery<IReadOnlyCollection<MerchantPriceResponse>>;
