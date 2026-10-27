using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Usage;

namespace Pricely.Modules.AiEngine.Presentation.Usage;

/// <summary>Web searches per provider over [fromUtc, toUtc[ (query string, optional).</summary>
internal sealed class GetWebSearchUsage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "aiengine/usage/web-search",
            async (DateTime? fromUtc, DateTime? toUtc, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<WebSearchUsageResponse>> result = await sender.Send(new GetWebSearchUsageQuery(fromUtc, toUtc), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.AiUsage);
    }
}
