using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");

        builder.HasKey(o => o.Id);

        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.IsComparable);

        builder.Property(o => o.ProductPage).HasMaxLength(2000);

        builder.Property(o => o.MerchantProductReference).HasMaxLength(Offer.MerchantProductReferenceMaxLength);

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(o => o.ConfirmationMethod).HasConversion<string>().HasMaxLength(30);

        builder.ComplexProperty(o => o.Evidence, evidence =>
        {
            evidence.Property(e => e.ObservedGtin).HasColumnName("evidence_observed_gtin").HasMaxLength(14);
            evidence.Property(e => e.ObservedBrand).HasColumnName("evidence_observed_brand").HasMaxLength(150);
            evidence.Property(e => e.ObservedManufacturerReference)
                .HasColumnName("evidence_observed_manufacturer_reference")
                .HasMaxLength(100);
            evidence.Property(e => e.AiSimilarity).HasColumnName("evidence_ai_similarity").HasPrecision(5, 4);
            evidence.Property(e => e.ObservedTitle).HasColumnName("evidence_observed_title").HasMaxLength(MatchEvidence.ObservedTitleMaxLength);
            evidence.Property(e => e.AiRationale).HasColumnName("evidence_ai_rationale").HasMaxLength(MatchEvidence.AiRationaleMaxLength);
        });

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(o => o.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductId references the Catalog module: plain column, no foreign key.
        builder.HasIndex(o => new { o.ProductId, o.MerchantId }).IsUnique();

        builder.HasIndex(o => o.Status);
    }
}
