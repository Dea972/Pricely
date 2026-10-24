using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Domain.Users;

public sealed class UserRoleChangedDomainEvent(Guid userId, string newRole) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
    public string NewRole { get; init; } = newRole;
}
