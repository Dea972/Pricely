namespace Pricely.Modules.AiEngine.Application.Usage;

// Costs in USD. An unpriced call consumed tokens without an applicable price (model missing from the catalog or
// without a price on that day): it counts for 0 in the costs, hence the number of unpriced calls next to them.

public class UsageTotals
{
    public int Calls { get; init; }

    public int FailedCalls { get; init; }

    public int UnpricedCalls { get; init; }

    public long InputTokens { get; init; }

    public long InputCachedTokens { get; init; }

    public long InputCacheCreationTokens { get; init; }

    public long OutputTokens { get; init; }

    public long TotalTokens { get; init; }

    public double AverageResponseTimeMs { get; init; }

    public decimal Cost { get; init; }
}

public sealed class UsageSummaryResponse : UsageTotals;

public sealed class ModelUsageResponse : UsageTotals
{
    public string ModelProvider { get; init; }

    public string ModelName { get; init; }
}

public sealed class FeatureUsageResponse : UsageTotals
{
    public string FeatureCode { get; init; }
}

public sealed class DailyUsageResponse : UsageTotals
{
    public DateTime Day { get; init; }
}

public sealed class AgentUsageResponse : UsageTotals
{
    public string AgentName { get; init; }
}

public sealed class ToolUsageResponse
{
    public string AgentName { get; init; }

    public string ToolName { get; init; }

    public bool IsReadOnly { get; init; }

    /// <summary>Tool runs (during a turn for a read tool, after approval for a write tool).</summary>
    public int Calls { get; init; }

    public int FailedCalls { get; init; }

    public double AverageDurationMs { get; init; }

    /// <summary>Write tools: actions approved and run, approved but failed, rejected.</summary>
    public int Executed { get; init; }

    public int FailedAfterApproval { get; init; }

    public int Rejected { get; init; }

    public double AverageDecisionDelayMs { get; init; }
}
