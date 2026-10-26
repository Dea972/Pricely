using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Offers.GetProductOffers;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class GetProductOffersQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetProductOffersQuery, ProductOffersResponse>
{
    public async Task<Result<ProductOffersResponse>> Handle(GetProductOffersQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Median of the latest price of each confirmed offer, in the most common currency of those offers.
        const string sql =
            $"""
             WITH latest AS (
                 SELECT
                     o.id AS offer_id,
                     o.merchant_id,
                     o.product_page,
                     o.status,
                     o.confirmation_method,
                     o.proposed_on_utc,
                     lp.price_amount,
                     lp.price_currency,
                     lp.observed_on_utc,
                     lp.is_flagged_as_anomaly
                 FROM pricing.offers o
                 LEFT JOIN LATERAL (
                     SELECT po.price_amount, po.price_currency, po.observed_on_utc, po.is_flagged_as_anomaly
                     FROM pricing.price_observations po
                     WHERE po.offer_id = o.id
                     ORDER BY po.observed_on_utc DESC
                     LIMIT 1
                 ) lp ON TRUE
                 WHERE o.product_id = @ProductId
                   AND o.status <> 'Rejected'
             ),
             reference AS (
                 SELECT
                     price_currency,
                     CAST(percentile_cont(0.5) WITHIN GROUP (ORDER BY price_amount) AS numeric(18, 2)) AS median
                 FROM latest
                 WHERE status = 'Confirmed' AND price_amount IS NOT NULL
                 GROUP BY price_currency
                 ORDER BY COUNT(*) DESC, price_currency
                 LIMIT 1
             )
             SELECT
                 l.offer_id AS {nameof(ProductOfferRow.OfferId)},
                 m.id AS {nameof(ProductOfferRow.MerchantId)},
                 m.name AS {nameof(ProductOfferRow.MerchantName)},
                 l.product_page AS {nameof(ProductOfferRow.ProductPage)},
                 l.status AS {nameof(ProductOfferRow.Status)},
                 l.confirmation_method AS {nameof(ProductOfferRow.ConfirmationMethod)},
                 l.price_amount AS {nameof(ProductOfferRow.LatestAmount)},
                 l.price_currency AS {nameof(ProductOfferRow.LatestCurrency)},
                 l.observed_on_utc AS {nameof(ProductOfferRow.LatestObservedOnUtc)},
                 COALESCE(l.is_flagged_as_anomaly, FALSE) AS {nameof(ProductOfferRow.LatestIsFlaggedAsAnomaly)},
                 r.median AS {nameof(ProductOfferRow.MedianAmount)},
                 r.price_currency AS {nameof(ProductOfferRow.MedianCurrency)},
                 CASE WHEN r.median > 0 AND l.price_currency = r.price_currency
                      THEN ROUND((l.price_amount - r.median) / r.median, 4)
                 END AS {nameof(ProductOfferRow.DeviationFromMedian)}
             FROM latest l
             INNER JOIN pricing.merchants m ON m.id = l.merchant_id
             LEFT JOIN reference r ON TRUE
             ORDER BY l.status, l.price_amount NULLS LAST, l.proposed_on_utc
             """;

        List<ProductOfferRow> rows = (await connection.QueryAsync<ProductOfferRow>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken))).AsList();

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

        ProductOfferRow? first = rows.Count > 0 ? rows[0] : null;

        return new ProductOffersResponse(request.ProductId, first?.MedianAmount, first?.MedianCurrency?.Trim(), offers);
    }

    internal sealed class ProductOfferRow
    {
        internal Guid OfferId { get; init; }

        internal Guid MerchantId { get; init; }

        internal string MerchantName { get; init; } = string.Empty;

        internal string ProductPage { get; init; } = string.Empty;

        internal string Status { get; init; } = string.Empty;

        internal string? ConfirmationMethod { get; init; }

        internal decimal? LatestAmount { get; init; }

        internal string? LatestCurrency { get; init; }

        internal DateTime? LatestObservedOnUtc { get; init; }

        internal bool LatestIsFlaggedAsAnomaly { get; init; }

        internal decimal? MedianAmount { get; init; }

        internal string? MedianCurrency { get; init; }

        internal decimal? DeviationFromMedian { get; init; }
    }
}
