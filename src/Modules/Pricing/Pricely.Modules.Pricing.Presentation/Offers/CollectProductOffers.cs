using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class CollectProductOffers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "pricing/products/{productId:guid}/collect",
            async (Guid productId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<CollectProductOffersResponse> result = await sender.Send(
                    new CollectProductOffersCommand(productId),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Offers);
    }
}
