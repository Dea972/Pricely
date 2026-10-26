using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Merchants.GetMerchants;

internal sealed class GetMerchantsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetMerchantsQuery, IReadOnlyCollection<MerchantResponse>>
{
    public async Task<Result<IReadOnlyCollection<MerchantResponse>>> Handle(
        GetMerchantsQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(MerchantResponse.Id)},
                 name AS {nameof(MerchantResponse.Name)},
                 website AS {nameof(MerchantResponse.Website)},
                 is_active AS {nameof(MerchantResponse.IsActive)},
                 search_template AS {nameof(MerchantResponse.SearchTemplate)}
             FROM pricing.merchants
             ORDER BY name
             """;

        IEnumerable<MerchantResponse> merchants = await connection.QueryAsync<MerchantResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<MerchantResponse>>(merchants.ToList());
    }
}
