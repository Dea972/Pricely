using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.Merchants.RegisterMerchant;

namespace Pricely.Modules.Pricing.Presentation.Merchants;

internal sealed class RegisterMerchant : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pricing/merchants", async (Request request, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(
                new RegisterMerchantCommand(request.Name, request.Website),
                cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Merchants);
    }

    internal sealed class Request
    {
        public string Name { get; init; }

        public string Website { get; init; }
    }
}
