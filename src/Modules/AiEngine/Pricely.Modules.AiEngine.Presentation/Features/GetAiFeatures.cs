using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Features.GetAiFeatures;

namespace Pricely.Modules.AiEngine.Presentation.Features;

internal sealed class GetAiFeatures : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("aiengine/features", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<AiFeatureResponse>> result =
                await sender.Send(new GetAiFeaturesQuery(), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.AiFeatures);
    }
}
