namespace Pricely.Modules.Agentic.Domain.Actions;

public interface IPendingActionRepository
{
    Task<PendingAction?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(PendingAction action);
}
