using Microsoft.EntityFrameworkCore;
using Pricely.Common.Infrastructure.Inbox;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;
using Pricely.Modules.Catalog.Infrastructure.Categories;
using Pricely.Modules.Catalog.Infrastructure.Products;

namespace Pricely.Modules.Catalog.Infrastructure.Database;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<ProductCategory> ProductCategories { get; set; }

    internal DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Catalog);

        // Case-insensitive text columns (emails, names, codes): PostgreSQL text comparisons are case-sensitive (ADR-012).
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}
