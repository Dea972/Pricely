using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.GetUserPermissions;
using MediatR;

namespace Pricely.Modules.Users.Infrastructure.Authorization;

internal sealed class PermissionService(ISender sender) : IPermissionService
{
    public async Task<Result<PermissionsResponse>> GetUserPermissionsAsync(string identityId)
    {
        return await sender.Send(new GetUserPermissionsQuery(identityId));
    }
}
