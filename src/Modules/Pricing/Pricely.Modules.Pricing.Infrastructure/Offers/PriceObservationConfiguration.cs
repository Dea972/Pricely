using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class PriceObservationConfiguration : IEntityTypeConfiguration<PriceObservation>
{
    public void Configure(EntityTypeBuilder<PriceObservation> builder)
    {
        builder.ToTable("price_observations");

        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.DomainEvents);

        builder.ComplexProperty(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_amount").HasPrecision(18, 2);
            price.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3).IsFixedLength();
        });

        builder.Property(p => p.Availability).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Offer>()
            .WithMany()
            .HasForeignKey(p => p.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.OfferId, p.ObservedOnUtc }).IsDescending(false, true);
    }
}
