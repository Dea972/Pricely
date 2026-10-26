using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;

internal sealed class GetProductPriceComparisonQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetProductPriceComparisonQuery, IReadOnlyCollection<MerchantPriceResponse>>
{
    public async Task<Result<IReadOnlyCollection<MerchantPriceResponse>>> Handle(
        GetProductPriceComparisonQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 o.id AS {nameof(MerchantPriceResponse.OfferId)},
                 m.id AS {nameof(MerchantPriceResponse.MerchantId)},
                 m.name AS {nameof(MerchantPriceResponse.MerchantName)},
                 o.product_page AS {nameof(MerchantPriceResponse.ProductPage)},
                 latest.price_amount AS {nameof(MerchantPriceResponse.Amount)},
                 latest.price_currency AS {nameof(MerchantPriceResponse.Currency)},
                 latest.availability AS {nameof(MerchantPriceResponse.Availability)},
                 latest.observed_on_utc AS {nameof(MerchantPriceResponse.ObservedOnUtc)},
                 latest.is_flagged_as_anomaly AS {nameof(MerchantPriceResponse.IsFlaggedAsAnomaly)}
             FROM pricing.offers o
             INNER JOIN pricing.merchants m ON m.id = o.merchant_id
             JOIN LATERAL (
                 SELECT po.price_amount, po.price_currency, po.availability, po.observed_on_utc, po.is_flagged_as_anomaly
                 FROM pricing.price_observations po
                 WHERE po.offer_id = o.id
                 ORDER BY po.observed_on_utc DESC
                 LIMIT 1
             ) latest ON TRUE
             WHERE o.product_id = @ProductId
               AND o.status = 'Confirmed'
               AND m.is_active
             ORDER BY latest.price_amount
             """;

        IEnumerable<MerchantPriceResponse> prices = await connection.QueryAsync<MerchantPriceResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<MerchantPriceResponse>>(prices.ToList());
    }
}
