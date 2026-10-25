using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Models.GetModels;

namespace Pricely.Modules.AiEngine.Presentation.Models;

internal sealed class GetModels : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("aiengine/models", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<ModelResponse>> result = await sender.Send(new GetModelsQuery(), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.AiModels);
    }
}
