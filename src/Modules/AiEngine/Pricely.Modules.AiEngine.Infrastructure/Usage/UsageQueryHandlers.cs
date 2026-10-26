using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Usage;

namespace Pricely.Modules.AiEngine.Infrastructure.Usage;

internal sealed class GetUsageSummaryQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsageSummaryQuery, UsageSummaryResponse>
{
    public async Task<Result<UsageSummaryResponse>> Handle(GetUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             """;

        return await connection.QuerySingleAsync<UsageSummaryResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));
    }
}

internal sealed class GetUsageByModelQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsageByModelQuery, IReadOnlyCollection<ModelUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<ModelUsageResponse>>> Handle(
        GetUsageByModelQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 c.model_provider AS {nameof(ModelUsageResponse.ModelProvider)},
                 c.model_name AS {nameof(ModelUsageResponse.ModelName)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             GROUP BY c.model_provider, c.model_name
             ORDER BY {nameof(ModelUsageResponse.Cost)} DESC, {nameof(ModelUsageResponse.Calls)} DESC
             """;

        IEnumerable<ModelUsageResponse> rows = await connection.QueryAsync<ModelUsageResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ModelUsageResponse>>(rows.ToList());
    }
}

internal sealed class GetUsageByFeatureQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsageByFeatureQuery, IReadOnlyCollection<FeatureUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<FeatureUsageResponse>>> Handle(
        GetUsageByFeatureQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 c.feature_code AS {nameof(FeatureUsageResponse.FeatureCode)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             GROUP BY c.feature_code
             ORDER BY {nameof(FeatureUsageResponse.Cost)} DESC, {nameof(FeatureUsageResponse.Calls)} DESC
             """;

        IEnumerable<FeatureUsageResponse> rows = await connection.QueryAsync<FeatureUsageResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<FeatureUsageResponse>>(rows.ToList());
    }
}

internal sealed class GetDailyUsageQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetDailyUsageQuery, IReadOnlyCollection<DailyUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<DailyUsageResponse>>> Handle(
        GetDailyUsageQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 CAST(CAST(c.called_on_utc AT TIME ZONE 'UTC' AS date) AS timestamp) AS {nameof(DailyUsageResponse.Day)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             GROUP BY CAST(c.called_on_utc AT TIME ZONE 'UTC' AS date)
             ORDER BY {nameof(DailyUsageResponse.Day)}
             """;

        IEnumerable<DailyUsageResponse> rows = await connection.QueryAsync<DailyUsageResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<DailyUsageResponse>>(rows.ToList());
    }
}
