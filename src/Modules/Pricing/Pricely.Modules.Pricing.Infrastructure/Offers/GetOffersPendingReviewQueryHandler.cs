using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;

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
                 latest.price_currency AS {nameof(PendingOfferResponse.LatestCurrency)}
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

        return Result.Success<IReadOnlyCollection<PendingOfferResponse>>(offers.ToList());
    }
}
