using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Models.SetModelPrice;

namespace Pricely.Modules.AiEngine.Presentation.Models;

internal sealed class SetModelPrice : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "aiengine/models/{modelId:guid}/prices",
            async (Guid modelId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new SetModelPriceCommand(
                        modelId,
                        request.ValidFrom,
                        request.InputPerMillion,
                        request.CachedInputPerMillion,
                        request.CacheWritePerMillion,
                        request.OutputPerMillion),
                    cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.AiModels);
    }

    internal sealed class Request
    {
        /// <summary>First UTC day of the price.</summary>
        public DateOnly ValidFrom { get; init; }

        public decimal InputPerMillion { get; init; }

        public decimal CachedInputPerMillion { get; init; }

        public decimal CacheWritePerMillion { get; init; }

        public decimal OutputPerMillion { get; init; }
    }
}
