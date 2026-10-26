using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.AiEngine.Application.Models.ProposeModelPrices;

namespace Pricely.Modules.AiEngine.Presentation.Models;

/// <summary>The current prices found for the models of the catalogue (LiteLLM, else the AI); nothing is saved.</summary>
internal sealed class ProposeModelPrices : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "aiengine/models/price-proposals",
            async (ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<ModelPriceProposalResponse>> result = await sender.Send(new ProposeModelPricesCommand(), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.AiModels);
    }
}
