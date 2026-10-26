using System.Data.Common;
using Dapper;
using Pricely.Common.Infrastructure.Data;
using Pricely.Modules.Pricing.Application.Offers;
using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;
using Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

/// <summary>
/// Reads the offers and their price observations with SQL (Dapper): the latest observation of each offer is a lateral
/// join and the median a window aggregate, which belong to the database, not to the Application layer. The products
/// belong to Catalog: the rows carry their identifier only.
/// </summary>
internal sealed class OfferReadStore(IDbConnectionFactory dbConnectionFactory) : IOfferReadStore
{
    public async Task<IReadOnlyCollection<PriceAnomalyRow>> GetPriceAnomaliesAsync(
        DateTime? sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            $"""
             SELECT
                 po.id AS {nameof(PriceAnomalyRow.PriceObservationId)},
                 o.id AS {nameof(PriceAnomalyRow.OfferId)},
                 o.product_id AS {nameof(PriceAnomalyRow.ProductId)},
                 m.name AS {nameof(PriceAnomalyRow.MerchantName)},
                 o.product_page AS {nameof(PriceAnomalyRow.ProductPage)},
                 o.status AS {nameof(PriceAnomalyRow.OfferStatus)},
                 po.price_amount AS {nameof(PriceAnomalyRow.Amount)},
                 po.price_currency AS {nameof(PriceAnomalyRow.Currency)},
                 po.observed_on_utc AS {nameof(PriceAnomalyRow.ObservedOnUtc)}
             FROM pricing.price_observations po
             INNER JOIN pricing.offers o ON o.id = po.offer_id
             INNER JOIN pricing.merchants m ON m.id = o.merchant_id
             WHERE po.is_flagged_as_anomaly
               AND (CAST(@SinceUtc AS timestamptz) IS NULL OR po.observed_on_utc >= @SinceUtc)
             ORDER BY po.observed_on_utc DESC
             LIMIT @Limit
             """;

        return await QueryAsync<PriceAnomalyRow>(sql, new { SinceUtc = sinceUtc, Limit = limit }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PriceHistoryEntryResponse>> GetPriceHistoryAsync(
        Guid productId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            $"""
             SELECT
                 o.id AS {nameof(PriceHistoryEntryResponse.OfferId)},
                 m.id AS {nameof(PriceHistoryEntryResponse.MerchantId)},
                 m.name AS {nameof(PriceHistoryEntryResponse.MerchantName)},
                 po.price_amount AS {nameof(PriceHistoryEntryResponse.Amount)},
                 po.price_currency AS {nameof(PriceHistoryEntryResponse.Currency)},
                 po.availability AS {nameof(PriceHistoryEntryResponse.Availability)},
                 po.observed_on_utc AS {nameof(PriceHistoryEntryResponse.ObservedOnUtc)},
                 po.is_flagged_as_anomaly AS {nameof(PriceHistoryEntryResponse.IsFlaggedAsAnomaly)}
             FROM pricing.price_observations po
             INNER JOIN pricing.offers o ON o.id = po.offer_id
             INNER JOIN pricing.merchants m ON m.id = o.merchant_id
             WHERE o.product_id = @ProductId
               AND o.status = @ConfirmedStatus
               AND (CAST(@FromUtc AS timestamptz) IS NULL OR po.observed_on_utc >= @FromUtc)
               AND (CAST(@ToUtc AS timestamptz) IS NULL OR po.observed_on_utc < @ToUtc)
             ORDER BY po.observed_on_utc DESC
             LIMIT @Limit
             """;

        return await QueryAsync<PriceHistoryEntryResponse>(
            sql,
            new { ProductId = productId, FromUtc = fromUtc, ToUtc = toUtc, Limit = limit, ConfirmedStatus = nameof(MatchStatus.Confirmed) },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<MerchantPriceResponse>> GetPriceComparisonAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
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

        return await QueryAsync<MerchantPriceResponse>(sql, new { ProductId = productId }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PendingOfferRow>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        const string sql =
            $"""
             SELECT
                 o.id AS {nameof(PendingOfferRow.OfferId)},
                 o.product_id AS {nameof(PendingOfferRow.ProductId)},
                 m.id AS {nameof(PendingOfferRow.MerchantId)},
                 m.name AS {nameof(PendingOfferRow.MerchantName)},
                 o.product_page AS {nameof(PendingOfferRow.ProductPage)},
                 o.merchant_product_reference AS {nameof(PendingOfferRow.MerchantProductReference)},
                 o.evidence_ai_similarity AS {nameof(PendingOfferRow.AiSimilarity)},
                 o.proposed_on_utc AS {nameof(PendingOfferRow.ProposedOnUtc)},
                 latest.price_amount AS {nameof(PendingOfferRow.LatestAmount)},
                 latest.price_currency AS {nameof(PendingOfferRow.LatestCurrency)},
                 o.evidence_observed_title AS {nameof(PendingOfferRow.ObservedTitle)},
                 o.evidence_observed_gtin AS {nameof(PendingOfferRow.ObservedGtin)},
                 o.evidence_observed_brand AS {nameof(PendingOfferRow.ObservedBrand)},
                 o.evidence_observed_manufacturer_reference AS {nameof(PendingOfferRow.ObservedManufacturerReference)},
                 o.evidence_ai_rationale AS {nameof(PendingOfferRow.AiRationale)}
             FROM pricing.offers o
             INNER JOIN pricing.merchants m ON m.id = o.merchant_id
             LEFT JOIN LATERAL (
                 SELECT po.price_amount, po.price_currency
                 FROM pricing.price_observations po
                 WHERE po.offer_id = o.id
                 ORDER BY po.observed_on_utc DESC
                 LIMIT 1
             ) latest ON TRUE
             WHERE o.status = 'Proposed'
             ORDER BY o.proposed_on_utc
             """;

        return await QueryAsync<PendingOfferRow>(sql, new { }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOfferRow>> GetProductOffersAsync(Guid productId, CancellationToken cancellationToken = default)
    {
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
             ORDER BY CASE l.status WHEN 'Confirmed' THEN 0 ELSE 1 END, l.price_amount NULLS LAST, l.proposed_on_utc
             """;

        return await QueryAsync<ProductOfferRow>(sql, new { ProductId = productId }, cancellationToken);
    }

    private async Task<IReadOnlyCollection<T>> QueryAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        return (await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).AsList();
    }
}
