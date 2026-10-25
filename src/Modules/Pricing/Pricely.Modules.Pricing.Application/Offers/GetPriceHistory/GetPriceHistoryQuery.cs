using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;

/// <summary>
/// Price observations of the confirmed offers of a product in [FromUtc, ToUtc[, most recent first.
/// </summary>
public sealed record GetPriceHistoryQuery(Guid ProductId, DateTime? FromUtc, DateTime? ToUtc, int Limit)
    : IQuery<IReadOnlyCollection<PriceHistoryEntryResponse>>
{
    public const int MaxLimit = 500;
}
