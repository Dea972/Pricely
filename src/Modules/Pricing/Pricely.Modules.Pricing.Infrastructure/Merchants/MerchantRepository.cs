using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.Merchants;

internal sealed class MerchantRepository(PricingDbContext context) : IMerchantRepository
{
    public Task<Merchant?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Merchants.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Merchant>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await context.Merchants.Where(m => ids.Contains(m.Id)).ToListAsync(cancellationToken);

    public void Insert(Merchant merchant) => context.Merchants.Add(merchant);
}
