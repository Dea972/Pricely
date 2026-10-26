using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;
using Pricely.Modules.Agentic.Infrastructure.Actions;
using Pricely.Modules.Agentic.Infrastructure.Conversations;

namespace Pricely.Modules.Agentic.Infrastructure.Database;

/// <summary>
/// Conversations and pending actions, in the <c>agentic</c> schema. The outbox carries the decisions on actions.
/// </summary>
public sealed class AgenticDbContext(DbContextOptions<AgenticDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<AgentConversation> Conversations { get; set; }

    internal DbSet<PendingAction> PendingActions { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrentUpdateException("The conversation or the action was changed in the meantime.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two turns of a conversation appending the same message sequence.
            throw new ConcurrentUpdateException("The conversation was changed in the meantime.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Agentic);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new AgentConversationConfiguration());
        modelBuilder.ApplyConfiguration(new AgentMessageConfiguration());
        modelBuilder.ApplyConfiguration(new PendingActionConfiguration());
    }
}
