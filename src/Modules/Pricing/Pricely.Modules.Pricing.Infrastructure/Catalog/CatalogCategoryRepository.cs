using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.Catalog;

internal sealed class CatalogCategoryRepository(PricingDbContext context) : ICatalogCategoryRepository
{
    public Task<CatalogCategory?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.CatalogCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Insert(CatalogCategory category) => context.CatalogCategories.Add(category);
}
