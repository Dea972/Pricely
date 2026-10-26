using System.Data.Common;
using Dapper;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Application.Users.GetUserPermissions;

internal sealed class GetUserPermissionsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUserPermissionsQuery, PermissionsResponse>
{
    public async Task<Result<PermissionsResponse>> Handle(
        GetUserPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Récupérer les permissions du rôle
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

        // Récupérer les permissions utilisateur (pour rôle Custom)
        const string userPermissionsSql =
            $"""
             SELECT DISTINCT
                 u.id AS {nameof(UserPermission.UserId)},
                 up.permission_code AS {nameof(UserPermission.Permission)}
             FROM users.users u
             JOIN users.user_permissions up ON up.user_id = u.id
             WHERE u.identity_id = @IdentityId
             """;

        List<UserPermission> rolePermissions = (await connection.QueryAsync<UserPermission>(rolePermissionsSql, request)).AsList();
        List<UserPermission> userPermissions = (await connection.QueryAsync<UserPermission>(userPermissionsSql, request)).AsList();


        // Combiner les deux listes
        var allPermissions = rolePermissions
            .Concat(userPermissions)
            .DistinctBy(p => p.Permission)
            .ToList();

        if (!allPermissions.Any())
        {
            return Result.Failure<PermissionsResponse>(UserErrors.NotFound(request.IdentityId));
        }

        return new PermissionsResponse(allPermissions[0].UserId, allPermissions.Select(p => p.Permission).ToHashSet());
    }

    internal sealed class UserPermission
    {
        internal Guid UserId { get; init; }

        internal string Permission { get; init; } = string.Empty;
    }
}
