using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Users.Application.Users.GetUserPermissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Pricely.Modules.Users.Presentation.Users;

internal sealed class GetUserPermissions : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/permissions", async (
            HttpContext context,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            string identityId = context.User.GetIdentityId();

            var query = new GetUserPermissionsQuery(identityId);
            Result<PermissionsResponse> result = await sender.Send(query, cancellationToken);

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .RequireAuthorization() // ← Juste authentifié (pas besoin de permission spécifique pour voir ses propres permissions)
        .WithTags(Tags.Users)
        .WithName("GetUserPermissions")
        .WithDescription("Get current user permissions");
    }
}
