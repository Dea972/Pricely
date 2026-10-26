using System.Security.Claims;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Exceptions;

namespace Pricely.Common.Infrastructure.Authentication;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract identity information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?.FindFirst(CustomClaims.UserId)?.Value;

        return Guid.TryParse(userId, out Guid parsedUserId) ?
            parsedUserId :
            throw new PricelyException("User identifier is unavailable");
    }

    /// <summary>
    /// The internal user id, or null when the caller could not be resolved to a Pricely user (see the claims transformation).
    /// </summary>
    public static Guid? FindUserId(this ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirst(CustomClaims.UserId)?.Value, out Guid userId) ? userId : null;

    public static string GetIdentityId(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst(CustomClaims.Sub)?.Value ??
               throw new PricelyException("User identity is unavailable");
    }

    public static IdentityProfile GetIdentityProfile(this ClaimsPrincipal principal)
    {
        return new IdentityProfile(
            principal.GetIdentityId(),
            principal.FindFirst(CustomClaims.Email)?.Value,
            principal.FindFirst(CustomClaims.GivenName)?.Value,
            principal.FindFirst(CustomClaims.FamilyName)?.Value,
            principal.FindFirst(CustomClaims.PreferredUserName)?.Value);
    }

    public static HashSet<string> GetPermissions(this ClaimsPrincipal? principal)
    {
        IEnumerable<Claim> permissionClaims = principal?.FindAll(CustomClaims.Permission) ??
                                              throw new PricelyException("Permissions are unavailable");

        return permissionClaims.Select(c => c.Value).ToHashSet();
    }

    /// <summary>
    /// Scopes granted to the token (the <c>scope</c> claim is a space-separated list).
    /// </summary>
    public static HashSet<string> GetScopes(this ClaimsPrincipal? principal)
    {
        IEnumerable<Claim> scopeClaims = principal?.FindAll(CustomClaims.Scope) ?? [];

        return scopeClaims
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);
    }
}
