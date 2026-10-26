using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantSearch;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class ConfigureMerchantSearch : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "pricing/merchants/{merchantId:guid}/search",
            async (Guid merchantId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(new ConfigureMerchantSearchCommand(merchantId, request.SearchTemplate), cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Merchants);
    }

    internal sealed class Request
    {
        /// <summary>Search page of the merchant's site with <c>{query}</c>; empty to remove it.</summary>
        public string? SearchTemplate { get; init; }
    }
}
