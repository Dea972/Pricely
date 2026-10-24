using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Products.UpdateProduct;

namespace Pricely.Modules.Catalog.Presentation.Products;

internal sealed class UpdateProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "catalog/products/{productId:guid}",
            async (Guid productId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result result = await sender.Send(
                    new UpdateProductCommand(
                        productId,
                        request.Name,
                        request.Brand,
                        request.Gtin,
                        request.ManufacturerReference,
                        request.ProductCategoryId),
                    cancellationToken);

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
        .RequireAuthorization()
        .WithTags(Tags.Products);
    }

    internal sealed class Request
    {
        public string Name { get; init; }

        public string Brand { get; init; }

        public string? Gtin { get; init; }

        public string? ManufacturerReference { get; init; }

        public Guid ProductCategoryId { get; init; }
    }
}
