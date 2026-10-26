namespace Pricely.Modules.AiEngine.Application.Usage;

/// <summary>
/// Shared SQL of the usage statistics, computed on the fly from <c>aiengine.llm_calls</c>.
/// </summary>
internal static class UsageSql
{
    internal const string Totals =
        $"""
         COUNT(*) AS {nameof(UsageTotals.Calls)},
         COUNT(*) FILTER (WHERE NOT c.is_success) AS {nameof(UsageTotals.FailedCalls)},
         COUNT(*) FILTER (WHERE c.is_success AND c.total_cost IS NULL) AS {nameof(UsageTotals.UnpricedCalls)},
         COALESCE(SUM(CAST(c.input_tokens AS bigint)), 0) AS {nameof(UsageTotals.InputTokens)},
         COALESCE(SUM(CAST(c.input_cached_tokens AS bigint)), 0) AS {nameof(UsageTotals.InputCachedTokens)},
         COALESCE(SUM(CAST(c.input_cache_creation_tokens AS bigint)), 0) AS {nameof(UsageTotals.InputCacheCreationTokens)},
         COALESCE(SUM(CAST(c.output_tokens AS bigint)), 0) AS {nameof(UsageTotals.OutputTokens)},
         COALESCE(SUM(CAST(c.input_tokens AS bigint) + c.input_cached_tokens + c.input_cache_creation_tokens + c.output_tokens), 0)
             AS {nameof(UsageTotals.TotalTokens)},
         COALESCE(AVG(CAST(c.response_time_ms AS double precision)), 0) AS {nameof(UsageTotals.AverageResponseTimeMs)},
         CAST(COALESCE(SUM(c.total_cost), 0) AS decimal(18, 6)) AS {nameof(UsageTotals.Cost)}
         """;

    internal const string PeriodFilter =
        """
        (CAST(@FromUtc AS timestamptz) IS NULL OR c.called_on_utc >= @FromUtc)
        AND (CAST(@ToUtc AS timestamptz) IS NULL OR c.called_on_utc < @ToUtc)
        """;
}
