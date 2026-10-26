using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Infrastructure.Data;
using Pricely.Modules.Users.Application.Users;
using Pricely.Modules.Users.Application.Users.GetAllUsers;
using Pricely.Modules.Users.Application.Users.GetUser;

namespace Pricely.Modules.Users.Infrastructure.Users;

/// <summary>
/// Reads the users with SQL (Dapper): joins over the role and permission tables, kept out of the Application layer.
/// </summary>
internal sealed class UserReadStore(IDbConnectionFactory dbConnectionFactory) : IUserReadStore
{
    public async Task<UserResponse?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(UserResponse.Id)},
                 email AS {nameof(UserResponse.Email)},
                 first_name AS {nameof(UserResponse.FirstName)},
                 last_name AS {nameof(UserResponse.LastName)}
             FROM users.users
             WHERE id = @UserId
             """;

        return await connection.QuerySingleOrDefaultAsync<UserResponse>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<UserDetailResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string usersSql =
            $"""
            SELECT
                u.id AS {nameof(UserRow.Id)},
                u.email AS {nameof(UserRow.Email)},
                u.first_name AS {nameof(UserRow.FirstName)},
                u.last_name AS {nameof(UserRow.LastName)},
                ur.role_name AS {nameof(UserRow.Role)}
            FROM users.users u
            LEFT JOIN users.user_roles ur ON ur.user_id = u.id
            ORDER BY u.email
            """;

        // Permissions granted by the role plus the ones granted to the user (Custom role).
        const string permissionsSql =
            $"""
            SELECT ur.user_id AS {nameof(PermissionRow.UserId)}, rp.permission_code AS {nameof(PermissionRow.Permission)}
            FROM users.user_roles ur
            JOIN users.role_permissions rp ON rp.role_name = ur.role_name
            UNION
            SELECT up.user_id, up.permission_code
            FROM users.user_permissions up
            """;

        List<UserRow> users = (await connection.QueryAsync<UserRow>(
            new CommandDefinition(usersSql, cancellationToken: cancellationToken))).AsList();

        ILookup<Guid, string> permissions = (await connection.QueryAsync<PermissionRow>(
                new CommandDefinition(permissionsSql, cancellationToken: cancellationToken)))
            .ToLookup(p => p.UserId, p => p.Permission);

        return
        [
            .. users.Select(u => new UserDetailResponse(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role ?? string.Empty,
                permissions[u.Id].Order(StringComparer.Ordinal).ToList()))
        ];
    }

    public async Task<PermissionsResponse?> GetPermissionsAsync(string identityId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Permissions of the role
        const string rolePermissionsSql =
            $"""
             SELECT DISTINCT
                 u.id AS {nameof(UserPermission.UserId)},
                 rp.permission_code AS {nameof(UserPermission.Permission)}
             FROM users.users u
             JOIN users.user_roles ur ON ur.user_id = u.id
             JOIN users.role_permissions rp ON rp.role_name = ur.role_name
             WHERE u.identity_id = @IdentityId
             """;

        // Permissions of the user (Custom role)
        const string userPermissionsSql =
            $"""
             SELECT DISTINCT
                 u.id AS {nameof(UserPermission.UserId)},
                 up.permission_code AS {nameof(UserPermission.Permission)}
             FROM users.users u
             JOIN users.user_permissions up ON up.user_id = u.id
             WHERE u.identity_id = @IdentityId
             """;

        var parameters = new { IdentityId = identityId };

        List<UserPermission> rolePermissions = (await connection.QueryAsync<UserPermission>(
            new CommandDefinition(rolePermissionsSql, parameters, cancellationToken: cancellationToken))).AsList();
        List<UserPermission> userPermissions = (await connection.QueryAsync<UserPermission>(
            new CommandDefinition(userPermissionsSql, parameters, cancellationToken: cancellationToken))).AsList();

        List<UserPermission> allPermissions = [.. rolePermissions.Concat(userPermissions).DistinctBy(p => p.Permission)];

        return allPermissions.Count == 0
            ? null
            : new PermissionsResponse(allPermissions[0].UserId, allPermissions.Select(p => p.Permission).ToHashSet());
    }

    internal sealed class UserRow
    {
        internal Guid Id { get; init; }

        internal string Email { get; init; } = string.Empty;

        internal string FirstName { get; init; } = string.Empty;

        internal string LastName { get; init; } = string.Empty;

        internal string? Role { get; init; }
    }

    internal sealed class PermissionRow
    {
        internal Guid UserId { get; init; }

        internal string Permission { get; init; } = string.Empty;
    }

    internal sealed class UserPermission
    {
        internal Guid UserId { get; init; }

        internal string Permission { get; init; } = string.Empty;
    }
}
