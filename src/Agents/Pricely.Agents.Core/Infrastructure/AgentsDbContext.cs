using Microsoft.EntityFrameworkCore;
using Pricely.Agents.Core.Domain;

namespace Pricely.Agents.Core.Infrastructure;

/// <summary>
/// Conversations and pending actions of the agents, in the <c>agents</c> schema. No outbox: the agents publish no event.
/// </summary>
public sealed class AgentsDbContext(DbContextOptions<AgentsDbContext> options) : DbContext(options)
{
    public const string Schema = "agents";

    internal DbSet<AgentConversation> Conversations { get; set; }

    internal DbSet<PendingAction> PendingActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AgentConversation>(builder =>
        {
            builder.ToTable("conversations");
            builder.HasKey(c => c.Id);
            builder.Ignore(c => c.DomainEvents);
            builder.Property(c => c.AgentName).HasMaxLength(50);
            builder.Property(c => c.Title).HasMaxLength(AgentConversation.TitleMaxLength);
            builder.HasIndex(c => new { c.UserId, c.LastActivityOnUtc });

            builder.HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationId);
            builder.Navigation(c => c.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<AgentMessage>(builder =>
        {
            builder.ToTable("messages");
            builder.HasKey(m => m.Id);
            // Id set by the domain: otherwise EF updates instead of inserting a message added to a tracked conversation.
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            builder.Property(m => m.Content).HasColumnType("text");
            builder.HasIndex(m => new { m.ConversationId, m.Sequence }).IsUnique();
        });

        modelBuilder.Entity<PendingAction>(builder =>
        {
            builder.ToTable("pending_actions");
            builder.HasKey(a => a.Id);
            builder.Ignore(a => a.DomainEvents);
            builder.Property(a => a.ToolName).HasMaxLength(100);
            builder.Property(a => a.Arguments).HasColumnType("text");
            builder.Property(a => a.RequiredScope).HasMaxLength(50);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.Result).HasColumnType("text");
            builder.Property(a => a.Error).HasMaxLength(2000);
            builder.HasIndex(a => a.ConversationId);

            // Same schema: an action belongs to its conversation.
            builder.HasOne<AgentConversation>().WithMany().HasForeignKey(a => a.ConversationId);
        });
    }
}
