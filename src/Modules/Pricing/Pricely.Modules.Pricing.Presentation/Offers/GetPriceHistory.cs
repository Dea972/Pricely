using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class GetPriceHistory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "pricing/products/{productId:guid}/price-history",
            async (Guid productId, DateTime? fromUtc, DateTime? toUtc, int? limit, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<PriceHistoryEntryResponse>> result = await sender.Send(
                    new GetPriceHistoryQuery(productId, fromUtc, toUtc, Math.Clamp(limit ?? 100, 1, GetPriceHistoryQuery.MaxLimit)),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Offers);
    }
}
