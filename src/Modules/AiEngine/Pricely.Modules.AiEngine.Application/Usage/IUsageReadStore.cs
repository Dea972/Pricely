namespace Pricely.Modules.AiEngine.Application.Usage;

/// <summary>
/// Read side of the usage statistics (queries), computed on the fly from the journal of the LLM calls and of the agents.
/// Implemented in Infrastructure.
/// </summary>
public interface IUsageReadStore
{
    Task<UsageSummaryResponse> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    Task<UsageSummaryResponse> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ModelUsageResponse>> GetByModelAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FeatureUsageResponse>> GetByFeatureAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DailyUsageResponse>> GetDailyAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AgentUsageResponse>> GetByAgentAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ToolUsageResponse>> GetByToolAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
