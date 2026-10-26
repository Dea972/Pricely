using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Catalog.Domain.Categories;

namespace Pricely.Modules.Catalog.Infrastructure.Categories;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(c => c.Id);

        builder.Ignore(c => c.DomainEvents);

        // citext: category names are unique ignoring case
        builder.Property(c => c.Name).HasMaxLength(ProductCategory.NameMaxLength).HasColumnType("citext");

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.ParentId, c.Name }).IsUnique();
    }
}
