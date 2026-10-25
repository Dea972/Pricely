using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.Catalog;

internal sealed class CatalogProductRepository(PricingDbContext context) : ICatalogProductRepository
{
    public Task<CatalogProduct?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.CatalogProducts.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> GetAllIdsAsync(CancellationToken cancellationToken = default) =>
        await context.CatalogProducts.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken);

    public void Insert(CatalogProduct product) => context.CatalogProducts.Add(product);
}
