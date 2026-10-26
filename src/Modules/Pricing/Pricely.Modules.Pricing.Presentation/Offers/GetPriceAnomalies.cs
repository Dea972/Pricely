using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class GetPriceAnomalies : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "pricing/price-anomalies",
            async (DateTime? sinceUtc, int? limit, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<PriceAnomalyResponse>> result = await sender.Send(
                    new GetPriceAnomaliesQuery(sinceUtc, Math.Clamp(limit ?? 50, 1, GetPriceAnomaliesQuery.MaxLimit)),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Offers);
    }
}
