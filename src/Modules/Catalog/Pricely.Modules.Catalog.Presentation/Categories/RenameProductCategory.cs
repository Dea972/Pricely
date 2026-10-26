using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Categories.RenameProductCategory;

namespace Pricely.Modules.Catalog.Presentation.Categories;

internal sealed class RenameProductCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "catalog/categories/{productCategoryId:guid}",
            async (Guid productCategoryId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new RenameProductCategoryCommand(productCategoryId, request.Name),
                    cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.ProductCategories);
    }

    internal sealed class Request
    {
        public string Name { get; init; }
    }
}
