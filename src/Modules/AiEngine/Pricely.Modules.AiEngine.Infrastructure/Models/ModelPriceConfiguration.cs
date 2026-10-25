using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Infrastructure.Models;

internal sealed class ModelPriceConfiguration : IEntityTypeConfiguration<ModelPrice>
{
    private const int PricePrecision = 18;
    private const int PriceScale = 6;

    public void Configure(EntityTypeBuilder<ModelPrice> builder)
    {
        builder.ToTable("model_prices");

        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.InputPerMillion).HasPrecision(PricePrecision, PriceScale);
        builder.Property(p => p.CachedInputPerMillion).HasPrecision(PricePrecision, PriceScale);
        builder.Property(p => p.CacheWritePerMillion).HasPrecision(PricePrecision, PriceScale);
        builder.Property(p => p.OutputPerMillion).HasPrecision(PricePrecision, PriceScale);

        builder.Property(p => p.Source).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => new { p.ModelId, p.ValidFrom }).IsUnique();
    }
}
