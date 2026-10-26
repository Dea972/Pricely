using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductOffers;

internal sealed class GetProductOffersQueryHandler(IOfferReadStore readStore)
    : IQueryHandler<GetProductOffersQuery, ProductOffersResponse>
{
    public async Task<Result<ProductOffersResponse>> Handle(GetProductOffersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProductOfferRow> rows = await readStore.GetProductOffersAsync(request.ProductId, cancellationToken);

        var offers = rows
            .Select(r => new ProductOfferResponse
            {
                OfferId = r.OfferId,
                MerchantId = r.MerchantId,
                MerchantName = r.MerchantName,
                ProductPage = r.ProductPage,
                Status = r.Status,
                ConfirmationMethod = r.ConfirmationMethod,
                LatestAmount = r.LatestAmount,
                LatestCurrency = r.LatestCurrency?.Trim(),
                LatestObservedOnUtc = r.LatestObservedOnUtc,
                LatestIsFlaggedAsAnomaly = r.LatestIsFlaggedAsAnomaly,
                DeviationFromMedian = r.DeviationFromMedian
            })
            .ToList();

        // The median is the same on every row: it is read from the first.
        ProductOfferRow? first = rows.FirstOrDefault();

        return new ProductOffersResponse(request.ProductId, first?.MedianAmount, first?.MedianCurrency?.Trim(), offers);
    }
}
