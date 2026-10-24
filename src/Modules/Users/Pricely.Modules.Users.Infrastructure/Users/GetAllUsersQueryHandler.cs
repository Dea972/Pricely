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

        const string sql =
            """
            SELECT
                u.id AS Id,
                u.email AS Email,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                r.name AS Role,
                u.created_on_utc AS CreatedAt,
                true AS IsActive
            FROM users.users u
            LEFT JOIN users.roles r ON u.id = r.user_id
            ORDER BY u.created_on_utc DESC
            """;

        // Récupérer tous les utilisateurs
        IEnumerable<UserDetailResponse> users = await connection.QueryAsync<UserDetailResponse>(sql);

        // Pour chaque utilisateur, récupérer ses permissions
        var usersList = users.ToList();
        foreach (UserDetailResponse? user in usersList)
        {
            const string permissionsSql =
                """
                SELECT permission
                FROM users.permissions
                WHERE user_id = @UserId
                """;

            IEnumerable<string> permissions = await connection.QueryAsync<string>(
                permissionsSql,
                new { UserId = user.Id });

            // Créer un nouvel objet avec les permissions
            int index = usersList.IndexOf(user);
            usersList[index] = user with { Permissions = permissions.ToList() };
        }

        return Result.Success<IEnumerable<UserDetailResponse>>(usersList);
    }
}
