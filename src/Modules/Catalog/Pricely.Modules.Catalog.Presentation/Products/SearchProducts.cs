using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Products.GetProduct;
using Pricely.Modules.Catalog.Application.Products.SearchProducts;

namespace Pricely.Modules.Catalog.Presentation.Products;

internal sealed class SearchProducts : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "catalog/products",
            async (string? text, Guid? productCategoryId, int? limit, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<ProductResponse>> result = await sender.Send(
                    new SearchProductsQuery(text, productCategoryId, Math.Clamp(limit ?? 20, 1, SearchProductsQuery.MaxLimit)),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.Products);
    }
}
