using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Usage;

namespace Pricely.Modules.AiEngine.Presentation.Usage;

/// <summary>
/// Usage statistics over [fromUtc, toUtc[ (query string, optional).
/// </summary>
internal sealed class UsageEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("aiengine/usage").RequireAuthorization().WithTags(Tags.AiUsage);

        group.MapGet("summary", async (DateTime? fromUtc, DateTime? toUtc, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<UsageSummaryResponse> result =
                await sender.Send(new GetUsageSummaryQuery(fromUtc, toUtc), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        });

        group.MapGet("by-model", async (DateTime? fromUtc, DateTime? toUtc, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<ModelUsageResponse>> result =
                await sender.Send(new GetUsageByModelQuery(fromUtc, toUtc), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        });

        group.MapGet("by-feature", async (DateTime? fromUtc, DateTime? toUtc, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<FeatureUsageResponse>> result =
                await sender.Send(new GetUsageByFeatureQuery(fromUtc, toUtc), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        });

        group.MapGet("daily", async (DateTime? fromUtc, DateTime? toUtc, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<DailyUsageResponse>> result =
                await sender.Send(new GetDailyUsageQuery(fromUtc, toUtc), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        });
    }
}
