using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class GetOffersPendingReview : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pricing/offers/pending-review", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<PendingOfferResponse>> result =
                await sender.Send(new GetOffersPendingReviewQuery(), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Offers);
    }
}
