using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Usage;

/// <summary>Totals of the calls made in [FromUtc, ToUtc[ (unbounded side when null).</summary>
public sealed record GetUsageSummaryQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<UsageSummaryResponse>;

/// <summary>Totals of the calls of one session (a conversation of an agent, a collection run).</summary>
public sealed record GetSessionUsageQuery(string SessionId) : IQuery<UsageSummaryResponse>;

/// <summary>Usage per provider and model in [FromUtc, ToUtc[, most expensive first.</summary>
public sealed record GetUsageByModelQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<IReadOnlyCollection<ModelUsageResponse>>;

/// <summary>Usage per AI feature in [FromUtc, ToUtc[, most expensive first.</summary>
public sealed record GetUsageByFeatureQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<IReadOnlyCollection<FeatureUsageResponse>>;

/// <summary>Usage per UTC day in [FromUtc, ToUtc[.</summary>
public sealed record GetDailyUsageQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<IReadOnlyCollection<DailyUsageResponse>>;

/// <summary>Model usage of the agents (calls made on behalf of an agent), by agent.</summary>
public sealed record GetUsageByAgentQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<IReadOnlyCollection<AgentUsageResponse>>;

/// <summary>Tool calls of the agents and decisions on their proposed actions, by agent and tool.</summary>
public sealed record GetUsageByToolQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<IReadOnlyCollection<ToolUsageResponse>>;

/// <summary>
/// Web searches per provider in [FromUtc, ToUtc[ (ADR-027): what they cost per search and in tokens, to compare
/// the providers.
/// </summary>
public sealed record GetWebSearchUsageQuery(DateTime? FromUtc, DateTime? ToUtc) : IQuery<IReadOnlyCollection<WebSearchUsageResponse>>;
