namespace Pricely.Modules.Pricing.Domain.Merchants;

public interface IMerchantRepository
{
    Task<Merchant?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Merchant>> GetManyAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    void Insert(Merchant merchant);
}
