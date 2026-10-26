using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class GetOffersPendingReviewQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetOffersPendingReviewQuery, IReadOnlyCollection<PendingOfferResponse>>
{
    public async Task<Result<IReadOnlyCollection<PendingOfferResponse>>> Handle(
        GetOffersPendingReviewQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 o.id AS {nameof(PendingOfferResponse.OfferId)},
                 o.product_id AS {nameof(PendingOfferResponse.ProductId)},
                 p.name AS {nameof(PendingOfferResponse.ProductName)},
                 m.id AS {nameof(PendingOfferResponse.MerchantId)},
                 m.name AS {nameof(PendingOfferResponse.MerchantName)},
                 o.product_page AS {nameof(PendingOfferResponse.ProductPage)},
                 o.merchant_product_reference AS {nameof(PendingOfferResponse.MerchantProductReference)},
                 o.evidence_ai_similarity AS {nameof(PendingOfferResponse.AiSimilarity)},
                 o.proposed_on_utc AS {nameof(PendingOfferResponse.ProposedOnUtc)},
                 latest.price_amount AS {nameof(PendingOfferResponse.LatestAmount)},
                 latest.price_currency AS {nameof(PendingOfferResponse.LatestCurrency)},
                 p.brand AS {nameof(PendingOfferResponse.ProductBrand)},
                 p.gtin AS {nameof(PendingOfferResponse.ProductGtin)},
                 p.manufacturer_reference AS {nameof(PendingOfferResponse.ProductManufacturerReference)},
                 o.evidence_observed_title AS {nameof(PendingOfferResponse.ObservedTitle)},
                 o.evidence_observed_gtin AS {nameof(PendingOfferResponse.ObservedGtin)},
                 o.evidence_observed_brand AS {nameof(PendingOfferResponse.ObservedBrand)},
                 o.evidence_observed_manufacturer_reference AS {nameof(PendingOfferResponse.ObservedManufacturerReference)},
                 o.evidence_ai_rationale AS {nameof(PendingOfferResponse.AiRationale)}
             FROM pricing.offers o
             INNER JOIN pricing.merchants m ON m.id = o.merchant_id
             INNER JOIN pricing.catalog_products p ON p.id = o.product_id
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

        IEnumerable<PendingOfferResponse> offers = await connection.QueryAsync<PendingOfferResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        // The comparison shown to the reviewer is the one of the matching policy (same normalization), not the SQL's.
        return Result.Success<IReadOnlyCollection<PendingOfferResponse>>(offers.Select(WithComparison).ToList());
    }

    private static PendingOfferResponse WithComparison(PendingOfferResponse offer)
    {
        MatchComparison comparison = OfferMatchingPolicy.Compare(
            new ProductIdentity(offer.ProductGtin, offer.ProductBrand, offer.ProductManufacturerReference),
            new MatchEvidence(offer.ObservedGtin, offer.ObservedBrand, offer.ObservedManufacturerReference, offer.AiSimilarity));

        return offer with
        {
            SameGtin = comparison.SameGtin,
            SameBrand = comparison.SameBrand,
            SameManufacturerReference = comparison.SameManufacturerReference
        };
    }
}
