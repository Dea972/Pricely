namespace Pricely.Modules.Agentic.Domain.Actions;

public interface IPendingActionRepository
{
    Task<PendingAction?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The decided actions of a conversation (their decisions are replayed to the model as notes).</summary>
    Task<IReadOnlyCollection<PendingAction>> GetDecidedAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>The actions of a batch, in the order they were proposed.</summary>
    Task<IReadOnlyList<PendingAction>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default);

    /// <summary>Whether an action of the batch was already approved or rejected.</summary>
    Task<bool> IsBatchDecidedAsync(Guid batchId, CancellationToken cancellationToken = default);

    void Insert(PendingAction action);
}
