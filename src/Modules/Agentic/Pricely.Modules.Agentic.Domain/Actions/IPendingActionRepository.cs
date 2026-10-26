namespace Pricely.Modules.Agentic.Domain.Actions;

public interface IPendingActionRepository
{
    Task<PendingAction?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The decided actions of a conversation (their decisions are replayed to the model as notes).</summary>
    Task<IReadOnlyCollection<PendingAction>> GetDecidedAsync(Guid conversationId, CancellationToken cancellationToken = default);

    void Insert(PendingAction action);
}
