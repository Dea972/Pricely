using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Products.GetProduct;

namespace Pricely.Modules.Catalog.Presentation.Products;

internal sealed class GetProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "catalog/products/{productId:guid}",
            async (Guid productId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<ProductResponse> result = await sender.Send(new GetProductQuery(productId), cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.Products);
    }
}
