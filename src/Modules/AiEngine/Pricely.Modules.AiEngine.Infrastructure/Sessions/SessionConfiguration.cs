using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.Sessions;

namespace Pricely.Modules.AiEngine.Infrastructure.Sessions;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);

        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.ExternalId).HasMaxLength(Session.ExternalIdMaxLength);

        builder.Property(s => s.SourceModule).HasMaxLength(50);

        builder.Property(s => s.UserId).HasMaxLength(100);

        builder.HasIndex(s => s.ExternalId).IsUnique();
    }
}
