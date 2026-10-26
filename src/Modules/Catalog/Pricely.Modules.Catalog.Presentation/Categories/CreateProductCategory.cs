using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Categories.CreateProductCategory;

namespace Pricely.Modules.Catalog.Presentation.Categories;

internal sealed class CreateProductCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("catalog/categories", async (Request request, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(
                new CreateProductCategoryCommand(request.Name, request.ParentId),
                cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.ProductCategories);
    }

    internal sealed class Request
    {
        public string Name { get; init; }

        public Guid? ParentId { get; init; }
    }
}
