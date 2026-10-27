using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Infrastructure.PageSnapshots;

internal sealed class PageSnapshotConfiguration : IEntityTypeConfiguration<PageSnapshot>
{
    public void Configure(EntityTypeBuilder<PageSnapshot> builder)
    {
        builder.ToTable("page_snapshots");

        builder.HasKey(s => s.Id);

        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.Address).HasMaxLength(PageSnapshot.AddressMaxLength);

        builder.Property(s => s.Content).HasMaxLength(PageSnapshot.ContentMaxLength);

        builder.PrimitiveCollection(s => s.DeclaredPrices)
            .HasField("_declaredPrices")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ElementType()
            .HasPrecision(18, 2);

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(s => s.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.MerchantId, s.FetchedOnUtc });
    }
}
