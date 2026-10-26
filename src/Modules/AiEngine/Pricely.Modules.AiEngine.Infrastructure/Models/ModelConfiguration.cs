using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Infrastructure.Models;

internal sealed class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("models");

        builder.HasKey(m => m.Id);

        builder.Ignore(m => m.DomainEvents);

        builder.Property(m => m.Provider).HasConversion<string>().HasMaxLength(30);

        // citext: provider model ids are compared ignoring case
        builder.Property(m => m.ProviderModelId).HasMaxLength(Model.ProviderModelIdMaxLength).HasColumnType("citext");

        builder.Property(m => m.Label).HasMaxLength(Model.LabelMaxLength);

        builder.HasIndex(m => new { m.Provider, m.ProviderModelId }).IsUnique();

        builder.HasMany(m => m.Prices)
            .WithOne()
            .HasForeignKey(p => p.ModelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Prices).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
