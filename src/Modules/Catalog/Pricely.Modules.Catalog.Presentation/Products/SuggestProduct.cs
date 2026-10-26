using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Products.SuggestProduct;

namespace Pricely.Modules.Catalog.Presentation.Products;

internal sealed class SuggestProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("catalog/products/suggestions", async (Request request, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<ProductSuggestionResponse> result = await sender.Send(new SuggestProductCommand(request.ProductPage), cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.Products);
    }

    internal sealed class Request
    {
        /// <summary>A product page of the manufacturer or of a merchant.</summary>
        public Uri ProductPage { get; init; } = default!;
    }
}
