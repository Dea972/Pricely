using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Application.Usage;

internal sealed class GetUsageSummaryQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetUsageSummaryQuery, UsageSummaryResponse>
{
    public async Task<Result<UsageSummaryResponse>> Handle(GetUsageSummaryQuery request, CancellationToken cancellationToken) =>
        await readStore.GetSummaryAsync(request.FromUtc, request.ToUtc, cancellationToken);
}

internal sealed class GetSessionUsageQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetSessionUsageQuery, UsageSummaryResponse>
{
    public async Task<Result<UsageSummaryResponse>> Handle(GetSessionUsageQuery request, CancellationToken cancellationToken) =>
        await readStore.GetSessionAsync(request.SessionId, cancellationToken);
}

internal sealed class GetUsageByModelQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetUsageByModelQuery, IReadOnlyCollection<ModelUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<ModelUsageResponse>>> Handle(
        GetUsageByModelQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetByModelAsync(request.FromUtc, request.ToUtc, cancellationToken));
}

internal sealed class GetUsageByFeatureQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetUsageByFeatureQuery, IReadOnlyCollection<FeatureUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<FeatureUsageResponse>>> Handle(
        GetUsageByFeatureQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetByFeatureAsync(request.FromUtc, request.ToUtc, cancellationToken));
}

internal sealed class GetDailyUsageQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetDailyUsageQuery, IReadOnlyCollection<DailyUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<DailyUsageResponse>>> Handle(
        GetDailyUsageQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetDailyAsync(request.FromUtc, request.ToUtc, cancellationToken));
}

internal sealed class GetUsageByAgentQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetUsageByAgentQuery, IReadOnlyCollection<AgentUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<AgentUsageResponse>>> Handle(
        GetUsageByAgentQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetByAgentAsync(request.FromUtc, request.ToUtc, cancellationToken));
}

internal sealed class GetWebSearchUsageQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetWebSearchUsageQuery, IReadOnlyCollection<WebSearchUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<WebSearchUsageResponse>>> Handle(
        GetWebSearchUsageQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetWebSearchAsync(request.FromUtc, request.ToUtc, cancellationToken));
}

internal sealed class GetUsageByToolQueryHandler(IUsageReadStore readStore)
    : IQueryHandler<GetUsageByToolQuery, IReadOnlyCollection<ToolUsageResponse>>
{
    public async Task<Result<IReadOnlyCollection<ToolUsageResponse>>> Handle(
        GetUsageByToolQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetByToolAsync(request.FromUtc, request.ToUtc, cancellationToken));
}
