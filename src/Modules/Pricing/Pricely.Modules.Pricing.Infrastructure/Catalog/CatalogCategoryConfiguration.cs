using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Infrastructure.Catalog;

internal sealed class CatalogCategoryConfiguration : IEntityTypeConfiguration<CatalogCategory>
{
    public void Configure(EntityTypeBuilder<CatalogCategory> builder)
    {
        builder.ToTable("catalog_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(200);
    }
}
