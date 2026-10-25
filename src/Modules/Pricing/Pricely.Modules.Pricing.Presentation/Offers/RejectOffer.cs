using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.RejectOffer;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class RejectOffer : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "pricing/offers/{offerId:guid}/reject",
            async (Guid offerId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new RejectOfferCommand(offerId), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.Offers);
    }
}
