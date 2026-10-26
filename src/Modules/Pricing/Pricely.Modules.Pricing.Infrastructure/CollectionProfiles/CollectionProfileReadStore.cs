using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Application.CollectionProfiles;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.CollectionProfiles;

internal sealed class CollectionProfileReadStore(PricingDbContext context) : ICollectionProfileReadStore
{
    public async Task<IReadOnlyCollection<CollectionProfileRow>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.CollectionProfiles
            .AsNoTracking()
            .Select(p => new CollectionProfileRow(p.ProductCategoryId, p.PreferredMerchants.Count, p.Instructions != null))
            .ToListAsync(cancellationToken);
}
