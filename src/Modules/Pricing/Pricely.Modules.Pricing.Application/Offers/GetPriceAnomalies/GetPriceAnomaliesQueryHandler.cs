using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;

/// <summary>The anomalies come from Pricing, the names of the products from Catalog (contract).</summary>
internal sealed class GetPriceAnomaliesQueryHandler(IOfferReadStore readStore, IProductReader productReader)
    : IQueryHandler<GetPriceAnomaliesQuery, IReadOnlyCollection<PriceAnomalyResponse>>
{
    public async Task<Result<IReadOnlyCollection<PriceAnomalyResponse>>> Handle(
        GetPriceAnomaliesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PriceAnomalyRow> rows = await readStore.GetPriceAnomaliesAsync(request.SinceUtc, request.Limit, cancellationToken);

        Result<Dictionary<Guid, ProductSummary>> products =
            await ProductLookup.LoadAsync(productReader, rows.Select(r => r.ProductId), cancellationToken);
        if (products.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<PriceAnomalyResponse>>(products.Error);
        }

        // An observation of a product Catalog no longer knows is left out, as an inner join would.
        IReadOnlyCollection<PriceAnomalyResponse> anomalies =
        [
            .. rows
                .Where(r => products.Value.ContainsKey(r.ProductId))
                .Select(r => new PriceAnomalyResponse
                {
                    PriceObservationId = r.PriceObservationId,
                    OfferId = r.OfferId,
                    ProductId = r.ProductId,
                    ProductName = products.Value[r.ProductId].Name,
                    MerchantName = r.MerchantName,
                    ProductPage = r.ProductPage,
                    OfferStatus = r.OfferStatus,
                    Amount = r.Amount,
                    Currency = r.Currency,
                    ObservedOnUtc = r.ObservedOnUtc
                })
        ];

        return Result.Success(anomalies);
    }
}
