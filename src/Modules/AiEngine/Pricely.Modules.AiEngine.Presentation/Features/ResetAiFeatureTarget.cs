using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Features.ResetAiFeatureTarget;

namespace Pricely.Modules.AiEngine.Presentation.Features;

internal sealed class ResetAiFeatureTarget : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "aiengine/features/{featureCode}/target",
            async (string featureCode, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new ResetAiFeatureTargetCommand(featureCode), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.AiFeatures);
    }
}
