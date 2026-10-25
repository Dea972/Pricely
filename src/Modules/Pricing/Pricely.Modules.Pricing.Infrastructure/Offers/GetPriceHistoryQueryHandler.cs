using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class GetPriceHistoryQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetPriceHistoryQuery, IReadOnlyCollection<PriceHistoryEntryResponse>>
{
    public async Task<Result<IReadOnlyCollection<PriceHistoryEntryResponse>>> Handle(
        GetPriceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT TOP (@Limit)
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
               AND (@FromUtc IS NULL OR po.observed_on_utc >= @FromUtc)
               AND (@ToUtc IS NULL OR po.observed_on_utc < @ToUtc)
             ORDER BY po.observed_on_utc DESC
             """;

        IEnumerable<PriceHistoryEntryResponse> entries = await connection.QueryAsync<PriceHistoryEntryResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    request.ProductId,
                    request.FromUtc,
                    request.ToUtc,
                    request.Limit,
                    ConfirmedStatus = nameof(MatchStatus.Confirmed)
                },
                cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<PriceHistoryEntryResponse>>(entries.ToList());
    }
}
