using Microsoft.AspNetCore.Http;
using Pricely.Common.Application.Authorization;

namespace Pricely.Common.Infrastructure.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId => httpContextAccessor.HttpContext?.User.FindUserId();

    public IReadOnlySet<string> Scopes => httpContextAccessor.HttpContext?.User.GetScopes() ?? [];
}
