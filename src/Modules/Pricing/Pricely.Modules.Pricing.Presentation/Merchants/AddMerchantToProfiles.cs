using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class AddMerchantToProfiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "pricing/merchants/{merchantId:guid}/profiles",
            async (Guid merchantId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new AddMerchantToProfilesCommand(merchantId, request.ProductCategoryIds ?? []),
                    cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Merchants);
    }

    internal sealed class Request
    {
        /// <summary>The typologies whose collection profile gets the merchant.</summary>
        public IReadOnlyList<Guid>? ProductCategoryIds { get; init; }
    }
}
