using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class SuggestMerchant : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "pricing/merchants/suggestions",
            async (Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<MerchantSuggestionResponse> result = await sender.Send(
                    new SuggestMerchantCommand(request.Website, request.Name),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Merchants);
    }

    internal sealed class Request
    {
        public string Website { get; init; } = string.Empty;

        public string? Name { get; init; }
    }
}
