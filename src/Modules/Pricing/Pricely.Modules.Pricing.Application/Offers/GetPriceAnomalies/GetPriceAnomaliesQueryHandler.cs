using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;

internal sealed class GetPriceAnomaliesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetPriceAnomaliesQuery, IReadOnlyCollection<PriceAnomalyResponse>>
{
    public async Task<Result<IReadOnlyCollection<PriceAnomalyResponse>>> Handle(
        GetPriceAnomaliesQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 po.id AS {nameof(PriceAnomalyResponse.PriceObservationId)},
                 o.id AS {nameof(PriceAnomalyResponse.OfferId)},
                 o.product_id AS {nameof(PriceAnomalyResponse.ProductId)},
                 p.name AS {nameof(PriceAnomalyResponse.ProductName)},
                 m.name AS {nameof(PriceAnomalyResponse.MerchantName)},
                 o.product_page AS {nameof(PriceAnomalyResponse.ProductPage)},
                 o.status AS {nameof(PriceAnomalyResponse.OfferStatus)},
                 po.price_amount AS {nameof(PriceAnomalyResponse.Amount)},
                 po.price_currency AS {nameof(PriceAnomalyResponse.Currency)},
                 po.observed_on_utc AS {nameof(PriceAnomalyResponse.ObservedOnUtc)}
             FROM pricing.price_observations po
             INNER JOIN pricing.offers o ON o.id = po.offer_id
             INNER JOIN pricing.merchants m ON m.id = o.merchant_id
             INNER JOIN pricing.catalog_products p ON p.id = o.product_id
             WHERE po.is_flagged_as_anomaly
               AND (CAST(@SinceUtc AS timestamptz) IS NULL OR po.observed_on_utc >= @SinceUtc)
             ORDER BY po.observed_on_utc DESC
             LIMIT @Limit
             """;

        IEnumerable<PriceAnomalyResponse> anomalies = await connection.QueryAsync<PriceAnomalyResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<PriceAnomalyResponse>>(anomalies.ToList());
    }
}
