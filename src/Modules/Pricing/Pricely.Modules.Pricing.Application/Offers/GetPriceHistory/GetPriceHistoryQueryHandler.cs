using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;

internal sealed class GetPriceHistoryQueryHandler(IOfferReadStore readStore)
    : IQueryHandler<GetPriceHistoryQuery, IReadOnlyCollection<PriceHistoryEntryResponse>>
{
    public async Task<Result<IReadOnlyCollection<PriceHistoryEntryResponse>>> Handle(
        GetPriceHistoryQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetPriceHistoryAsync(request.ProductId, request.FromUtc, request.ToUtc, request.Limit, cancellationToken));
}
