
using Pricely.Common.Domain;

namespace Pricely.Common.Application.Authorization;

public interface IPermissionService
{
    /// <summary>
    /// Returns the internal user id and the business permissions of the caller, creating the user on its first
    /// authenticated request (just-in-time provisioning).
    /// </summary>
    Task<Result<PermissionsResponse>> GetUserPermissionsAsync(IdentityProfile identity, CancellationToken cancellationToken = default);
}
