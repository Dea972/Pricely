using System;
using System.Collections.Generic;
using System.Text;
using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Domain.Users;

public sealed class UserRegisteredDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}
