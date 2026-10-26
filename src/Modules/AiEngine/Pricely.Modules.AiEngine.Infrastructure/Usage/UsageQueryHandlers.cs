using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Usage;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

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

internal sealed class GetUsageByAgentQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsageByAgentQuery, IReadOnlyCollection<AgentUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<AgentUsageResponse>>> Handle(
        GetUsageByAgentQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 c.agent_name AS {nameof(AgentUsageResponse.AgentName)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE c.agent_name IS NOT NULL AND {UsageSql.PeriodFilter}
             GROUP BY c.agent_name
             ORDER BY {nameof(AgentUsageResponse.Cost)} DESC, {nameof(AgentUsageResponse.Calls)} DESC
             """;

        IEnumerable<AgentUsageResponse> rows = await connection.QueryAsync<AgentUsageResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<AgentUsageResponse>>(rows.ToList());
    }
}

internal sealed class GetUsageByToolQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsageByToolQuery, IReadOnlyCollection<ToolUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<ToolUsageResponse>>> Handle(
        GetUsageByToolQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 a.agent_name AS {nameof(ToolUsageResponse.AgentName)},
                 a.tool_name AS {nameof(ToolUsageResponse.ToolName)},
                 BOOL_OR(a.is_read_only_tool) AS {nameof(ToolUsageResponse.IsReadOnly)},
                 COUNT(*) FILTER (WHERE a.kind = '{nameof(AgentActivityKind.ToolInvoked)}') AS {nameof(ToolUsageResponse.Calls)},
                 COUNT(*) FILTER (WHERE a.kind = '{nameof(AgentActivityKind.ToolInvoked)}' AND NOT a.is_success) AS {nameof(ToolUsageResponse.FailedCalls)},
                 COALESCE(AVG(CAST(a.duration_ms AS double precision)) FILTER (WHERE a.kind = '{nameof(AgentActivityKind.ToolInvoked)}'), 0)
                     AS {nameof(ToolUsageResponse.AverageDurationMs)},
                 COUNT(*) FILTER (WHERE a.decision = '{nameof(AgentActionDecision.Executed)}') AS {nameof(ToolUsageResponse.Executed)},
                 COUNT(*) FILTER (WHERE a.decision = '{nameof(AgentActionDecision.Failed)}') AS {nameof(ToolUsageResponse.FailedAfterApproval)},
                 COUNT(*) FILTER (WHERE a.decision = '{nameof(AgentActionDecision.Rejected)}') AS {nameof(ToolUsageResponse.Rejected)},
                 COALESCE(AVG(CAST(a.decision_delay_ms AS double precision)) FILTER (WHERE a.kind = '{nameof(AgentActivityKind.ActionDecided)}'), 0)
                     AS {nameof(ToolUsageResponse.AverageDecisionDelayMs)}
             FROM aiengine.agent_activities a
             WHERE (CAST(@FromUtc AS timestamptz) IS NULL OR a.occurred_on_utc >= @FromUtc)
               AND (CAST(@ToUtc AS timestamptz) IS NULL OR a.occurred_on_utc < @ToUtc)
             GROUP BY a.agent_name, a.tool_name
             ORDER BY {nameof(ToolUsageResponse.Calls)} DESC, a.agent_name, a.tool_name
             """;

        IEnumerable<ToolUsageResponse> rows = await connection.QueryAsync<ToolUsageResponse>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyCollection<ToolUsageResponse>>(rows.ToList());
    }
}
