using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;

namespace Pricely.Modules.Pricing.Presentation.Offers;

internal sealed class GetProductPriceComparison : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "pricing/products/{productId:guid}/prices",
            async (Guid productId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<MerchantPriceResponse>> result = await sender.Send(
                    new GetProductPriceComparisonQuery(productId),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.Offers);
    }
}
