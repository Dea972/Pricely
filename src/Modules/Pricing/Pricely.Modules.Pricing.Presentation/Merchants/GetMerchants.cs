using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class GetMerchants : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pricing/merchants", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<MerchantResponse>> result =
                await sender.Send(new GetMerchantsQuery(), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Merchants);
    }
}
