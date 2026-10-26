using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Infrastructure.Database;

namespace Pricely.Modules.Agentic.Infrastructure.Actions;

internal sealed class PendingActionRepository(AgenticDbContext context) : IPendingActionRepository
{
    public Task<PendingAction?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.PendingActions.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void Insert(PendingAction action) => context.PendingActions.Add(action);
}
