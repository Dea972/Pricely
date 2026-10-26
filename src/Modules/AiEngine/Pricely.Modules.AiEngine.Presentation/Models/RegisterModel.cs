using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Models.RegisterModel;

namespace Pricely.Modules.AiEngine.Presentation.Models;

internal sealed class RegisterModel : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("aiengine/models", async (Request request, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(
                new RegisterModelCommand(request.Provider, request.ProviderModelId, request.Label),
                cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.AiModels);
    }

    internal sealed class Request
    {
        public string Provider { get; init; }

        public string ProviderModelId { get; init; }

        public string Label { get; init; }
    }
}
