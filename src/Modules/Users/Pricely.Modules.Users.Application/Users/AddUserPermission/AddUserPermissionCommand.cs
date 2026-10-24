using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.AddUserPermission;

public sealed record AddUserPermissionCommand(Guid UserId, string PermissionCode) : ICommand;
