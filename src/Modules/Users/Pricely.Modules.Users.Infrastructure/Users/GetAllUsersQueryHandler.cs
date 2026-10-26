using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.GetAllUsers;

namespace Pricely.Modules.Users.Infrastructure.Users;

internal sealed class GetAllUsersQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetAllUsersQuery, IEnumerable<UserDetailResponse>>
{
    public async Task<Result<IEnumerable<UserDetailResponse>>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
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

        var response = users
            .Select(u => new UserDetailResponse(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role ?? string.Empty,
                permissions[u.Id].Order(StringComparer.Ordinal).ToList()))
            .ToList();

        return Result.Success<IEnumerable<UserDetailResponse>>(response);
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
}
