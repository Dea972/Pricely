using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Infrastructure.Products;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength);

        builder.Property(p => p.Brand).HasMaxLength(Product.BrandMaxLength);

        builder.Property(p => p.ManufacturerReference).HasMaxLength(Product.ManufacturerReferenceMaxLength);

        builder.Property(p => p.Gtin)
            .HasConversion(gtin => gtin!.Value, value => Gtin.Create(value).Value)
            .HasMaxLength(14);

        builder.HasIndex(p => p.Gtin).IsUnique();

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
