using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.CollectionProfiles.DefineCollectionProfile;

namespace Pricely.Modules.Pricing.Presentation.CollectionProfiles;

internal sealed class DefineCollectionProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "pricing/collection-profiles/{productCategoryId:guid}",
            async (Guid productCategoryId, Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await sender.Send(
                    new DefineCollectionProfileCommand(
                        productCategoryId,
                        request.PreferredMerchantIds ?? [],
                        request.Instructions),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Write)
        .WithTags(Tags.CollectionProfiles);
    }

    internal sealed class Request
    {
        /// <summary>Merchants to query first, by priority. Empty: inherit from the parent typology.</summary>
        public IReadOnlyList<Guid>? PreferredMerchantIds { get; init; }

        public string? Instructions { get; init; }
    }
}
