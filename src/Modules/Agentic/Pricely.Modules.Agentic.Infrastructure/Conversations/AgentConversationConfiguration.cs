using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Infrastructure.Conversations;

internal sealed class AgentConversationConfiguration : IEntityTypeConfiguration<AgentConversation>
{
    public void Configure(EntityTypeBuilder<AgentConversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);

        builder.Property(c => c.AgentName).HasMaxLength(AgentConversation.AgentNameMaxLength);
        builder.Property(c => c.Title).HasMaxLength(AgentConversation.TitleMaxLength);
        builder.HasIndex(c => new { c.UserId, c.LastActivityOnUtc });

        // Optimistic concurrency on xmin: two turns of the same conversation cannot both be saved.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasMany(c => c.Messages).WithOne().HasForeignKey(m => m.ConversationId);
        builder.Navigation(c => c.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AgentMessageConfiguration : IEntityTypeConfiguration<AgentMessage>
{
    public void Configure(EntityTypeBuilder<AgentMessage> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(m => m.Id);

        // Id set by the domain: otherwise EF updates instead of inserting a message added to a tracked conversation.
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Content).HasColumnType("text");
        builder.HasIndex(m => new { m.ConversationId, m.Sequence }).IsUnique();
    }
}
