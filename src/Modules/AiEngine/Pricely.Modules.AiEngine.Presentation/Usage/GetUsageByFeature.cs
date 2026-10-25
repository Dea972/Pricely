using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Usage;

namespace Pricely.Modules.AiEngine.Presentation.Usage;

/// <summary>Usage over [fromUtc, toUtc[ (query string, optional).</summary>
internal sealed class GetUsageByFeature : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "aiengine/usage/by-feature",
            async (DateTime? fromUtc, DateTime? toUtc, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<FeatureUsageResponse>> result = await sender.Send(new GetUsageByFeatureQuery(fromUtc, toUtc), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.AiUsage);
    }
}
