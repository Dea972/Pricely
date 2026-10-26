using Pricely.Common.Application.Authorization;
using Pricely.Modules.Users.Application.Users.GetAllUsers;
using Pricely.Modules.Users.Application.Users.GetUser;

namespace Pricely.Modules.Users.Application.Users;

/// <summary>
/// Read side of the users (queries): the data access is behind this port, implemented in Infrastructure.
/// </summary>
public interface IUserReadStore
{
    Task<UserResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Every user with their role and permissions (the ones of the role plus the ones granted to the user).</summary>
    Task<IReadOnlyCollection<UserDetailResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>The permissions of the user of this identity; null when the user is unknown or has none.</summary>
    Task<PermissionsResponse?> GetPermissionsAsync(string identityId, CancellationToken cancellationToken = default);
}
