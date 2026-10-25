namespace Pricely.Modules.Pricing.Domain.Offers;

public interface IPriceObservationRepository
{
    /// <summary>
    /// Latest price of every confirmed offer of the product, except <paramref name="excludedOfferId"/>.
    /// </summary>
    Task<IReadOnlyCollection<Money>> GetLatestComparablePricesAsync(
        Guid productId,
        Guid excludedOfferId,
        CancellationToken cancellationToken = default);

    void Insert(PriceObservation observation);
}
