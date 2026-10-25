using Pricely.Modules.AiEngine.Application.Usage;

namespace Pricely.Modules.AiEngine.Infrastructure.Usage;

/// <summary>
/// Shared SQL of the usage statistics, computed on the fly from <c>aiengine.llm_calls</c>.
/// </summary>
internal static class UsageSql
{
    internal const string Totals =
        $"""
         COUNT(*) AS {nameof(UsageTotals.Calls)},
         COALESCE(SUM(CASE WHEN c.is_success = 0 THEN 1 ELSE 0 END), 0) AS {nameof(UsageTotals.FailedCalls)},
         COALESCE(SUM(CASE WHEN c.is_success = 1 AND c.total_cost IS NULL THEN 1 ELSE 0 END), 0) AS {nameof(UsageTotals.UnpricedCalls)},
         COALESCE(SUM(CAST(c.input_tokens AS bigint)), 0) AS {nameof(UsageTotals.InputTokens)},
         COALESCE(SUM(CAST(c.input_cached_tokens AS bigint)), 0) AS {nameof(UsageTotals.InputCachedTokens)},
         COALESCE(SUM(CAST(c.input_cache_creation_tokens AS bigint)), 0) AS {nameof(UsageTotals.InputCacheCreationTokens)},
         COALESCE(SUM(CAST(c.output_tokens AS bigint)), 0) AS {nameof(UsageTotals.OutputTokens)},
         COALESCE(SUM(CAST(c.input_tokens AS bigint) + c.input_cached_tokens + c.input_cache_creation_tokens + c.output_tokens), 0)
             AS {nameof(UsageTotals.TotalTokens)},
         COALESCE(AVG(CAST(c.response_time_ms AS float)), 0) AS {nameof(UsageTotals.AverageResponseTimeMs)},
         CAST(COALESCE(SUM(c.total_cost), 0) AS decimal(18, 6)) AS {nameof(UsageTotals.Cost)}
         """;

    internal const string PeriodFilter =
        """
        (@FromUtc IS NULL OR c.called_on_utc >= @FromUtc)
        AND (@ToUtc IS NULL OR c.called_on_utc < @ToUtc)
        """;
}
