using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.DeactivateMerchant;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class DeactivateMerchant : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "pricing/merchants/{merchantId:guid}/deactivate",
            async (Guid merchantId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new DeactivateMerchantCommand(merchantId), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Merchants);
    }
}
