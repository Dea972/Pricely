using Microsoft.AspNetCore.Authorization;
using Pricely.Common.Infrastructure.Authentication;

namespace Pricely.Common.Infrastructure.Authorization;

internal sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        if (context.User.GetScopes().Contains(requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
