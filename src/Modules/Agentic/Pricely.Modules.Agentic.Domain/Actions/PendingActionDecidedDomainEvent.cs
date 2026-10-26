using Pricely.Common.Domain;

namespace Pricely.Modules.Agentic.Domain.Actions;

/// <summary>
/// A person approved or rejected a pending action, and its outcome is known (executed, failed or rejected).
/// </summary>
public sealed class PendingActionDecidedDomainEvent(Guid actionId) : DomainEvent
{
    public Guid ActionId { get; init; } = actionId;
}
