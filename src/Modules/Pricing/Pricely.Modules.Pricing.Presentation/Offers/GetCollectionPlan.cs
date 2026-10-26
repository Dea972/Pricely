using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class GetCollectionPlan : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "pricing/products/{productId:guid}/collection-plan",
            async (Guid productId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<CollectionPlanResponse> result = await sender.Send(
                    new GetCollectionPlanQuery(productId),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Offers);
    }
}
