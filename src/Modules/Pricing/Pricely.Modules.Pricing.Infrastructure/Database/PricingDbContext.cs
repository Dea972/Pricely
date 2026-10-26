using Microsoft.EntityFrameworkCore;
using Pricely.Common.Infrastructure.Inbox;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Infrastructure.Catalog;
using Pricely.Modules.Pricing.Infrastructure.CollectionProfiles;
using Pricely.Modules.Pricing.Infrastructure.Merchants;
using Pricely.Modules.Pricing.Infrastructure.Offers;

namespace Pricely.Modules.Pricing.Infrastructure.Database;

public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<Merchant> Merchants { get; set; }

    internal DbSet<CollectionProfile> CollectionProfiles { get; set; }

    internal DbSet<Offer> Offers { get; set; }

    internal DbSet<PriceObservation> PriceObservations { get; set; }

    internal DbSet<CatalogProduct> CatalogProducts { get; set; }

    internal DbSet<CatalogCategory> CatalogCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Pricing);

        // Case-insensitive text columns (emails, names, codes): PostgreSQL text comparisons are case-sensitive (ADR-012).
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new MerchantConfiguration());
        modelBuilder.ApplyConfiguration(new CollectionProfileConfiguration());
        modelBuilder.ApplyConfiguration(new OfferConfiguration());
        modelBuilder.ApplyConfiguration(new PriceObservationConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogCategoryConfiguration());
    }
}
