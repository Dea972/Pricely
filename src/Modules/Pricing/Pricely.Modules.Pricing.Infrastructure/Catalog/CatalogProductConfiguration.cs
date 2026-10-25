using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Infrastructure.Catalog;

internal sealed class CatalogProductConfiguration : IEntityTypeConfiguration<CatalogProduct>
{
    public void Configure(EntityTypeBuilder<CatalogProduct> builder)
    {
        builder.ToTable("catalog_products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(300);

        builder.Property(p => p.Brand).HasMaxLength(150);

        builder.Property(p => p.Gtin).HasMaxLength(14);

        builder.Property(p => p.ManufacturerReference).HasMaxLength(100);

        builder.HasIndex(p => p.ProductCategoryId);
    }
}
