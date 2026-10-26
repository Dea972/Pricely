using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Pricing.Application.CollectionProfiles.GetCollectionProfiles;

namespace Pricely.Modules.Pricing.Presentation.CollectionProfiles;

internal sealed class GetCollectionProfiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "pricing/collection-profiles",
            async (ISender sender, CancellationToken cancellationToken) =>
            {
                Result<IReadOnlyCollection<CollectionProfileSummaryResponse>> result = await sender.Send(
                    new GetCollectionProfilesQuery(),
                    cancellationToken);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
        .RequireAuthorization(PricelyScopes.Read)
        .WithTags(Tags.CollectionProfiles);
    }
}
