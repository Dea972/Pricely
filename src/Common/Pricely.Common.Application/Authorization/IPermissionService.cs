
using Pricely.Common.Domain;

namespace Pricely.Common.Application.Authorization;

public interface IPermissionService
{
    /// <summary>
    /// Resolves the caller: returns the internal user id and the business permissions of the identity, creating the
    /// user on its first authenticated request (just-in-time provisioning).
    /// </summary>
    Task<Result<PermissionsResponse>> ResolveCallerAsync(IdentityProfile identity, CancellationToken cancellationToken = default);
}
