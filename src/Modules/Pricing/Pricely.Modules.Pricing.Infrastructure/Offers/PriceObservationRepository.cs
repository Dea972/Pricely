using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class PriceObservationRepository(PricingDbContext context, IDbConnectionFactory dbConnectionFactory)
    : IPriceObservationRepository
{
    public async Task<IReadOnlyCollection<Money>> GetLatestComparablePricesAsync(
        Guid productId,
        Guid excludedOfferId,
        CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT latest.price_amount AS {nameof(PriceRow.Amount)}, latest.price_currency AS {nameof(PriceRow.Currency)}
             FROM pricing.offers o
             JOIN LATERAL (
                 SELECT po.price_amount, po.price_currency
                 FROM pricing.price_observations po
                 WHERE po.offer_id = o.id
                 ORDER BY po.observed_on_utc DESC
                 LIMIT 1
             ) latest ON TRUE
             WHERE o.product_id = @ProductId
               AND o.id <> @ExcludedOfferId
               AND o.status = 'Confirmed'
             """;

        IEnumerable<PriceRow> rows = await connection.QueryAsync<PriceRow>(
            new CommandDefinition(
                sql,
                new { ProductId = productId, ExcludedOfferId = excludedOfferId },
                cancellationToken: cancellationToken));

        return rows
            .Select(row => Money.Create(row.Amount, row.Currency))
            .Where(price => price.IsSuccess)
            .Select(price => price.Value)
            .ToList();
    }

    public void Insert(PriceObservation observation) => context.PriceObservations.Add(observation);

    private sealed record PriceRow(decimal Amount, string Currency);
}
