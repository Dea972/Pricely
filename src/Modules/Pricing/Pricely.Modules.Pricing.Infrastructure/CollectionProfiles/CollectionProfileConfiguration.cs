using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Infrastructure.CollectionProfiles;

internal sealed class CollectionProfileConfiguration : IEntityTypeConfiguration<CollectionProfile>
{
    public void Configure(EntityTypeBuilder<CollectionProfile> builder)
    {
        builder.ToTable("collection_profiles");

        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.Instructions).HasMaxLength(CollectionProfile.InstructionsMaxLength);

        // A product category belongs to the Catalog module: plain column, no foreign key.
        builder.HasIndex(p => p.ProductCategoryId).IsUnique();

        builder.OwnsMany(p => p.PreferredMerchants, preferredMerchant =>
        {
            preferredMerchant.ToTable("collection_profile_preferred_merchants");

            preferredMerchant.WithOwner().HasForeignKey("CollectionProfileId");

            preferredMerchant.HasKey("CollectionProfileId", nameof(PreferredMerchant.MerchantId));

            preferredMerchant.HasOne<Merchant>()
                .WithMany()
                .HasForeignKey(m => m.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Navigation(p => p.PreferredMerchants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
