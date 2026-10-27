using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Infrastructure.Database;

namespace Pricely.Modules.Agentic.Infrastructure.Actions;

internal sealed class PendingActionRepository(AgenticDbContext context) : IPendingActionRepository
{
    public Task<PendingAction?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.PendingActions.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<PendingAction>> GetDecidedAsync(Guid conversationId, CancellationToken cancellationToken = default) =>
        await context.PendingActions
            .AsNoTracking()
            .Where(a => a.ConversationId == conversationId && a.DecidedOnUtc != null && a.Status != PendingActionStatus.Approved)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PendingAction>> GetBatchAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        await context.PendingActions
            .Where(a => a.BatchId == batchId)
            .OrderBy(a => a.BatchSequence)
            .ToListAsync(cancellationToken);

    public Task<bool> IsBatchDecidedAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        context.PendingActions.AsNoTracking().AnyAsync(a => a.BatchId == batchId && a.Status != PendingActionStatus.Pending, cancellationToken);

    public void Insert(PendingAction action) => context.PendingActions.Add(action);
}
