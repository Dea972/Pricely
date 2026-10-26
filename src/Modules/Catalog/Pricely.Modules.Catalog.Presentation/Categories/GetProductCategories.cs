using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Categories.GetProductCategories;

namespace Pricely.Modules.Catalog.Presentation.Categories;

internal sealed class GetProductCategories : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("catalog/categories", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyCollection<ProductCategoryResponse>> result = await sender.Send(
                new GetProductCategoriesQuery(),
                cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.ProductCategories);
    }
}
