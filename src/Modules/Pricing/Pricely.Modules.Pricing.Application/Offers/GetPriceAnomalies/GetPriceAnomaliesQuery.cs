using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;

/// <summary>
/// Price observations flagged by the price guard since <see cref="SinceUtc"/>, most recent first.
/// </summary>
public sealed record GetPriceAnomaliesQuery(DateTime? SinceUtc, int Limit) : IQuery<IReadOnlyCollection<PriceAnomalyResponse>>
{
    public const int MaxLimit = 200;
}
