using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Domain.Users;

public sealed class UserPermissionAddedDomainEvent(Guid userId, string permissionCode) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
    public string PermissionCode { get; init; } = permissionCode;
}
