using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantProductPage;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class ConfigureMerchantProductPage : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "pricing/merchants/{merchantId:guid}/product-page",
            async (Guid merchantId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new ConfigureMerchantProductPageCommand(merchantId, request.ProductPageTemplate),
                    cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Merchants);
    }

    internal sealed class Request
    {
        /// <summary>Product page of the merchant's site with <c>{gtin13}</c> or <c>{gtin14}</c>; empty to remove it.</summary>
        public string? ProductPageTemplate { get; init; }
    }
}
