using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Features.SetAiFeatureTarget;

namespace Pricely.Modules.AiEngine.Presentation.Features;

internal sealed class SetAiFeatureTarget : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "aiengine/features/{featureCode}/target",
            async (string featureCode, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new SetAiFeatureTargetCommand(featureCode, request.ModelId),
                    cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.AiFeatures);
    }

    internal sealed class Request
    {
        public Guid ModelId { get; init; }
    }
}
