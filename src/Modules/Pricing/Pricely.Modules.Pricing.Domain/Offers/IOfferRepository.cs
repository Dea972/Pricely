namespace Pricely.Modules.Pricing.Domain.Offers;

public interface IOfferRepository
{
    Task<Offer?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Offer?> GetForProductAndMerchantAsync(
        Guid productId,
        Guid merchantId,
        CancellationToken cancellationToken = default);

    void Insert(Offer offer);
}
