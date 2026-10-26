using Microsoft.AspNetCore.Authorization;

namespace Pricely.Common.Infrastructure.Authorization;

internal sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}
