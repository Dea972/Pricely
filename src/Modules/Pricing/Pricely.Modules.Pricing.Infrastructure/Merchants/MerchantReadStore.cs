using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Application.Merchants;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.Merchants;

internal sealed class MerchantReadStore(PricingDbContext context) : IMerchantReadStore
{
    public async Task<IReadOnlyCollection<MerchantResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var merchants = await context.Merchants
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, m.Name, m.Website, m.IsActive, m.SearchTemplate, m.ProductPageTemplate })
            .ToListAsync(cancellationToken);

        // The address is stored as typed: the column is read back as text.
        return [.. merchants.Select(m => new MerchantResponse(m.Id, m.Name, m.Website.OriginalString, m.IsActive, m.SearchTemplate, m.ProductPageTemplate))];
    }
}
