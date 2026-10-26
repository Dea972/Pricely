using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Catalog.Application.Products.RegisterProduct;

namespace Pricely.Modules.Catalog.Presentation.Products;

internal sealed class RegisterProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("catalog/products", async (Request request, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(
                new RegisterProductCommand(
                    request.Name,
                    request.Brand,
                    request.Gtin,
                    request.ManufacturerReference,
                    request.ProductCategoryId),
                cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization(PricelyScopes.Write)
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
