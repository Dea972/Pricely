using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Infrastructure.Merchants;

internal sealed class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("merchants");

        builder.HasKey(m => m.Id);

        builder.Ignore(m => m.DomainEvents);

        // citext: merchant names are unique ignoring case
        builder.Property(m => m.Name).HasMaxLength(Merchant.NameMaxLength).HasColumnType("citext");

        builder.Property(m => m.Website).HasMaxLength(500);

        builder.Property(m => m.SearchTemplate).HasMaxLength(Merchant.SearchTemplateMaxLength);

        builder.Property(m => m.ProductPageTemplate).HasMaxLength(Merchant.ProductPageTemplateMaxLength);

        builder.HasIndex(m => m.Name).IsUnique();
    }
}
