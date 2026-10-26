using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.Features;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Infrastructure.Features;

internal sealed class FeatureSettingConfiguration : IEntityTypeConfiguration<FeatureSetting>
{
    public void Configure(EntityTypeBuilder<FeatureSetting> builder)
    {
        builder.ToTable("feature_settings");

        builder.HasKey(s => s.Id);

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.IsDefault);

        // citext: feature codes are compared ignoring case
        builder.Property(s => s.FeatureCode).HasMaxLength(FeatureSetting.FeatureCodeMaxLength).HasColumnType("citext");

        builder.HasIndex(s => s.FeatureCode).IsUnique();

        builder.HasOne<Model>()
            .WithMany()
            .HasForeignKey(s => s.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
