using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.RemoveUserPermission;

public sealed record RemoveUserPermissionCommand(Guid UserId, string PermissionCode) : ICommand;
