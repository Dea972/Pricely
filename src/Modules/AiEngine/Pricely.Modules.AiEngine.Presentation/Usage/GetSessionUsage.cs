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

/// <summary>Usage of one session: the calls journaled with this session id (the conversation id for an agent).</summary>
internal sealed class GetSessionUsage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "aiengine/usage/sessions/{sessionId}",
            async (string sessionId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<UsageSummaryResponse> result = await sender.Send(new GetSessionUsageQuery(sessionId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.AiUsage);
    }
}
