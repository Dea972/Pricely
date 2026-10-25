using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// A price seen on a merchant page at a given time. Kept outside <see cref="Offer"/> because the history grows quickly.
/// </summary>
public sealed class PriceObservation : Entity
{
    private PriceObservation()
    {
    }

    public Guid Id { get; private set; }

    public Guid OfferId { get; private set; }

    public Money Price { get; private set; }

    public Availability Availability { get; private set; }

    public DateTime ObservedOnUtc { get; private set; }

    public bool IsFlaggedAsAnomaly { get; private set; }

    public static Result<PriceObservation> Record(
        Offer offer,
        Money price,
        Availability availability,
        DateTime observedOnUtc,
        IReadOnlyCollection<Money> referencePrices)
    {
        if (offer.Status == MatchStatus.Rejected)
        {
            return Result.Failure<PriceObservation>(OfferErrors.Rejected(offer.Id));
        }

        PriceObservation observation = new()
        {
            Id = Guid.CreateVersion7(),
            OfferId = offer.Id,
            Price = price,
            Availability = availability,
            ObservedOnUtc = observedOnUtc,
            IsFlaggedAsAnomaly = PriceAnomalyPolicy.IsAnomalous(price, referencePrices)
        };

        if (observation.IsFlaggedAsAnomaly)
        {
            observation.Raise(new PriceAnomalyDetectedDomainEvent(observation.Id, offer.Id, offer.ProductId));
        }

        return observation;
    }
}
