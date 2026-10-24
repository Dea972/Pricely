using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.GetUserPermissions;

public sealed record GetUserPermissionsQuery(string IdentityId) : IQuery<PermissionsResponse>;
