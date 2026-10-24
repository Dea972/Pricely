using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid UserId, string RoleName) : ICommand;
