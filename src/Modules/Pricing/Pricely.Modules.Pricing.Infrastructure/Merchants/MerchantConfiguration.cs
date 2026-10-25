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

        builder.Property(m => m.Name).HasMaxLength(Merchant.NameMaxLength);

        builder.Property(m => m.Website).HasMaxLength(500);

        builder.HasIndex(m => m.Name).IsUnique();
    }
}
