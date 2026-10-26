using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.CollectionProfiles.GetEffectiveCollectionProfile;

namespace Pricely.Modules.Pricing.Presentation.CollectionProfiles;

internal sealed class GetEffectiveCollectionProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "pricing/collection-profiles/{productCategoryId:guid}/effective",
            async (Guid productCategoryId, ISender sender, CancellationToken cancellationToken) =>
            {
                Result<EffectiveCollectionProfileResponse> result = await sender.Send(
                    new GetEffectiveCollectionProfileQuery(productCategoryId),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.CollectionProfiles);
    }
}
