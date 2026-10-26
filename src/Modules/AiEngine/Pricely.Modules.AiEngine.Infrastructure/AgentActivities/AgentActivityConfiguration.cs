using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Infrastructure.AgentActivities;

internal sealed class AgentActivityConfiguration : IEntityTypeConfiguration<AgentActivity>
{
    public void Configure(EntityTypeBuilder<AgentActivity> builder)
    {
        builder.ToTable("agent_activities");

        builder.HasKey(a => a.Id);

        // Id of the reported event: a replayed event fails on the key instead of being recorded twice.
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Ignore(a => a.DomainEvents);

        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.SessionId).HasMaxLength(AgentActivity.SessionIdMaxLength);
        builder.Property(a => a.FeatureCode).HasMaxLength(AgentActivity.FeatureCodeMaxLength);
        builder.Property(a => a.AgentName).HasMaxLength(AgentActivity.AgentNameMaxLength);
        builder.Property(a => a.ToolName).HasMaxLength(AgentActivity.ToolNameMaxLength);
        builder.Property(a => a.UserId).HasMaxLength(AgentActivity.UserIdMaxLength);
        builder.Property(a => a.Decision).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.ErrorMessage).HasMaxLength(AgentActivity.ErrorMessageMaxLength);

        builder.HasIndex(a => a.OccurredOnUtc);
        builder.HasIndex(a => a.SessionId);
        builder.HasIndex(a => new { a.AgentName, a.ToolName });
    }
}
