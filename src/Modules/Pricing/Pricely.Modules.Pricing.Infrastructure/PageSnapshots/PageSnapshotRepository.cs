using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Domain.PageSnapshots;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.PageSnapshots;

internal sealed class PageSnapshotRepository(PricingDbContext context) : IPageSnapshotRepository
{
    public Task<PageSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.PageSnapshots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Insert(PageSnapshot snapshot) => context.PageSnapshots.Add(snapshot);
}
