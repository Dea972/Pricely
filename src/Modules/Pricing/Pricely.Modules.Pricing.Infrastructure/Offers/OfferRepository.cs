using Microsoft.EntityFrameworkCore;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Infrastructure.Database;

namespace Pricely.Modules.Pricing.Infrastructure.Offers;

internal sealed class OfferRepository(PricingDbContext context) : IOfferRepository
{
    public Task<Offer?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Offers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Offer?> GetForProductAndMerchantAsync(
        Guid productId,
        Guid merchantId,
        CancellationToken cancellationToken = default) =>
        context.Offers.FirstOrDefaultAsync(o => o.ProductId == productId && o.MerchantId == merchantId, cancellationToken);

    public void Insert(Offer offer) => context.Offers.Add(offer);
}
