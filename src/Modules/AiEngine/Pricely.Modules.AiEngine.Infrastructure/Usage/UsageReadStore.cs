using System.Data.Common;
using Dapper;
using Pricely.Common.Infrastructure.Data;
using Pricely.Modules.AiEngine.Application.Usage;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Infrastructure.Usage;

/// <summary>
/// Reads the usage statistics with SQL (Dapper): aggregations (FILTER, GROUP BY) over the journals, kept out of the
/// Application layer.
/// </summary>
internal sealed class UsageReadStore(IDbConnectionFactory dbConnectionFactory) : IUsageReadStore
{
    public Task<UsageSummaryResponse> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        QuerySingleAsync<UsageSummaryResponse>(
            $"""
             SELECT {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             """,
            new { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);

    public Task<UsageSummaryResponse> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        QuerySingleAsync<UsageSummaryResponse>(
            $"""
             SELECT {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE c.session_id = @SessionId
             """,
            new { SessionId = sessionId },
            cancellationToken);

    public Task<IReadOnlyCollection<ModelUsageResponse>> GetByModelAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        QueryAsync<ModelUsageResponse>(
            $"""
             SELECT
                 c.model_provider AS {nameof(ModelUsageResponse.ModelProvider)},
                 c.model_name AS {nameof(ModelUsageResponse.ModelName)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             GROUP BY c.model_provider, c.model_name
             ORDER BY {nameof(ModelUsageResponse.Cost)} DESC, {nameof(ModelUsageResponse.Calls)} DESC
             """,
            new { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);

    public Task<IReadOnlyCollection<FeatureUsageResponse>> GetByFeatureAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        QueryAsync<FeatureUsageResponse>(
            $"""
             SELECT
                 c.feature_code AS {nameof(FeatureUsageResponse.FeatureCode)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             GROUP BY c.feature_code
             ORDER BY {nameof(FeatureUsageResponse.Cost)} DESC, {nameof(FeatureUsageResponse.Calls)} DESC
             """,
            new { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);

    public Task<IReadOnlyCollection<DailyUsageResponse>> GetDailyAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        QueryAsync<DailyUsageResponse>(
            $"""
             SELECT
                 CAST(CAST(c.called_on_utc AT TIME ZONE 'UTC' AS date) AS timestamp) AS {nameof(DailyUsageResponse.Day)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE {UsageSql.PeriodFilter}
             GROUP BY CAST(c.called_on_utc AT TIME ZONE 'UTC' AS date)
             ORDER BY {nameof(DailyUsageResponse.Day)}
             """,
            new { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);

    public Task<IReadOnlyCollection<AgentUsageResponse>> GetByAgentAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        QueryAsync<AgentUsageResponse>(
            $"""
             SELECT
                 c.agent_name AS {nameof(AgentUsageResponse.AgentName)},
                 {UsageSql.Totals}
             FROM aiengine.llm_calls c
             WHERE c.agent_name IS NOT NULL AND {UsageSql.PeriodFilter}
             GROUP BY c.agent_name
             ORDER BY {nameof(AgentUsageResponse.Cost)} DESC, {nameof(AgentUsageResponse.Calls)} DESC
             """,
            new { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);

    public Task<IReadOnlyCollection<ToolUsageResponse>> GetByToolAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        QueryAsync<ToolUsageResponse>(
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
             """,
            new { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);

    private async Task<T> QuerySingleAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        return await connection.QuerySingleAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private async Task<IReadOnlyCollection<T>> QueryAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        return (await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).AsList();
    }
}
