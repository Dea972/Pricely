using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.CollectionProfiles;

internal sealed class CollectionProfileRepository(PricingDbContext context) : ICollectionProfileRepository
{
    public Task<CollectionProfile?> GetByCategoryAsync(
        Guid productCategoryId,
        CancellationToken cancellationToken = default) =>
        context.CollectionProfiles.FirstOrDefaultAsync(p => p.ProductCategoryId == productCategoryId, cancellationToken);

    public async Task<IReadOnlyCollection<CollectionProfile>> GetByCategoriesAsync(
        IReadOnlyCollection<Guid> productCategoryIds,
        CancellationToken cancellationToken = default) =>
        await context.CollectionProfiles
            .Where(p => productCategoryIds.Contains(p.ProductCategoryId))
            .ToListAsync(cancellationToken);

    public void Insert(CollectionProfile profile) => context.CollectionProfiles.Add(profile);
}
