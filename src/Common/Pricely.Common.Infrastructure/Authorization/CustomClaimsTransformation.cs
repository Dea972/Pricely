
using System.Security.Claims;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pricely.Common.Infrastructure.Authorization;

/// <summary>
/// Adds the business permissions and the internal user id to an authenticated principal. The user is created on
/// its first authenticated request.
/// An identity that cannot be resolved is left without permissions.
/// </summary>
internal sealed class CustomClaimsTransformation(IServiceScopeFactory serviceScopeFactory, ILogger<CustomClaimsTransformation> logger) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == CustomClaims.UserId))
        {
            return principal;
        }

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        IPermissionService permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        Result<PermissionsResponse> result = await permissionService.ResolveCallerAsync(principal.GetIdentityProfile());

        if (result.IsFailure)
        {
            // Not an exception: the caller is authenticated but unknown to Pricely, so it gets no permission and
            // the authorization policies answer 403.
            logger.LogWarning("The caller {IdentityId} could not be resolved: {Error}", principal.GetIdentityId(), result.Error);

            return principal;
        }

        var claimsIdentity = new ClaimsIdentity();

        claimsIdentity.AddClaim(new Claim(CustomClaims.UserId, result.Value.UserId.ToString()));

        foreach (string permission in result.Value.Permissions)
        {
            claimsIdentity.AddClaim(new Claim(CustomClaims.Permission, permission));
        }

        principal.AddIdentity(claimsIdentity);

        return principal;
    }
}
