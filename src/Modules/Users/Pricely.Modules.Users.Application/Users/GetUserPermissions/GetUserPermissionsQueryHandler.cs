using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Application.Users.GetUserPermissions;

internal sealed class GetUserPermissionsQueryHandler(IUserReadStore readStore)
    : IQueryHandler<GetUserPermissionsQuery, PermissionsResponse>
{
    public async Task<Result<PermissionsResponse>> Handle(
        GetUserPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        PermissionsResponse? permissions = await readStore.GetPermissionsAsync(request.IdentityId, cancellationToken);

        if (permissions is null)
        {
            return Result.Failure<PermissionsResponse>(UserErrors.NotFound(request.IdentityId));
        }

        return permissions;
    }
}
