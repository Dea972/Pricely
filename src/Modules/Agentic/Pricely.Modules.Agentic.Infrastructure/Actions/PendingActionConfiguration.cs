using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Agentic.Domain.Actions;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Infrastructure.Actions;

internal sealed class PendingActionConfiguration : IEntityTypeConfiguration<PendingAction>
{
    public void Configure(EntityTypeBuilder<PendingAction> builder)
    {
        builder.ToTable("pending_actions");
        builder.HasKey(a => a.Id);
        builder.Ignore(a => a.DomainEvents);

        builder.Property(a => a.AgentName).HasMaxLength(AgentConversation.AgentNameMaxLength);
        builder.Property(a => a.ToolName).HasMaxLength(PendingAction.ToolNameMaxLength);
        builder.Property(a => a.Arguments).HasColumnType("text");
        builder.Property(a => a.RequiredScope).HasMaxLength(PendingAction.ScopeMaxLength);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Result).HasColumnType("text");
        builder.Property(a => a.Error).HasMaxLength(PendingAction.ErrorMaxLength);
        builder.HasIndex(a => a.ConversationId);
        builder.HasIndex(a => a.BatchId);

        // Optimistic concurrency on xmin: an action is approved once, the tool runs once.
        builder.Property<uint>("Version").IsRowVersion();

        // Same schema: an action belongs to its conversation.
        builder.HasOne<AgentConversation>().WithMany().HasForeignKey(a => a.ConversationId);
    }
}
