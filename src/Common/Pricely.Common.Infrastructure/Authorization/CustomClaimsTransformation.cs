
using System.Security.Claims;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Pricely.Common.Infrastructure.Authorization;

/// <summary>
/// Adds the business permissions and the internal user id to an authenticated principal. The user is created on
/// its first authenticated request.
/// </summary>
internal sealed class CustomClaimsTransformation(IServiceScopeFactory serviceScopeFactory) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == CustomClaims.UserId))
        {
            return principal;
        }

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        IPermissionService permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        Result<PermissionsResponse> result = await permissionService.GetUserPermissionsAsync(principal.GetIdentityProfile());

        if (result.IsFailure)
        {
            throw new PricelyException(nameof(IPermissionService.GetUserPermissionsAsync), result.Error);
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
