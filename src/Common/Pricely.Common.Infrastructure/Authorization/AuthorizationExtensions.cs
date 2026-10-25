
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.Authorization;

namespace Pricely.Common.Infrastructure.Authorization;

internal static class AuthorizationExtensions
{
    internal static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        // One policy per OAuth scope (ADR-010), named after the scope: .RequireAuthorization(PricelyScopes.Read).
        services.AddAuthorizationBuilder()
            .AddPolicy(PricelyScopes.Read, policy => policy.RequireAuthenticatedUser().AddRequirements(new ScopeRequirement(PricelyScopes.Read)))
            .AddPolicy(PricelyScopes.Write, policy => policy.RequireAuthenticatedUser().AddRequirements(new ScopeRequirement(PricelyScopes.Write)))
            .AddPolicy(PricelyScopes.Review, policy => policy.RequireAuthenticatedUser().AddRequirements(new ScopeRequirement(PricelyScopes.Review)));

        return services;
    }
}
