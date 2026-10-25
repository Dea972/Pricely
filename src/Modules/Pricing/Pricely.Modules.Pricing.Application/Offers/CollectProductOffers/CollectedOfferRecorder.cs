using Pricely.Common.Application.Clock;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

/// <summary>
/// Turns a collected offer into an <see cref="Offer"/> (matching) and a <see cref="PriceObservation"/>.
/// Each collected offer is saved on its own so that one bad page does not discard the others.
/// </summary>
public sealed class CollectedOfferRecorder(
    IOfferRepository offerRepository,
    IPriceObservationRepository priceObservationRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<OfferRecording> RecordAsync(
        OfferCollectionPlan plan,
        CollectedOffer collectedOffer,
        CancellationToken cancellationToken)
    {
        Guid productId = plan.Request.Product.ProductId;

        Offer? offer = await offerRepository.GetForProductAndMerchantAsync(
            productId,
            collectedOffer.MerchantId,
            cancellationToken);

        if (offer?.Status == MatchStatus.Rejected)
        {
            return new OfferRecording(OfferRecordingKind.IgnoredRejectedOffer, IsFlaggedAsAnomaly: false);
        }

        OfferRecordingKind kind = OfferRecordingKind.ExistingOfferObserved;
        if (offer is null)
        {
            offer = Offer.Propose(
                new OfferCandidate(
                    productId,
                    collectedOffer.MerchantId,
                    collectedOffer.ProductPage,
                    collectedOffer.MerchantProductReference,
                    collectedOffer.Evidence),
                plan.ProductIdentity,
                dateTimeProvider.UtcNow);
            offerRepository.Insert(offer);
            kind = offer.IsComparable ? OfferRecordingKind.NewOfferConfirmed : OfferRecordingKind.NewOfferProposed;
        }

        Result<Money> price = Money.Create(collectedOffer.PriceAmount, collectedOffer.Currency);
        if (price.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new OfferRecording(OfferRecordingKind.IgnoredInvalidPrice, IsFlaggedAsAnomaly: false);
        }

        bool isFlaggedAsAnomaly = await RecordObservationAsync(offer, price.Value, collectedOffer, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new OfferRecording(kind, isFlaggedAsAnomaly);
    }

    private async Task<bool> RecordObservationAsync(
        Offer offer,
        Money price,
        CollectedOffer collectedOffer,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Money> referencePrices = await priceObservationRepository.GetLatestComparablePricesAsync(
            offer.ProductId,
            offer.Id,
            cancellationToken);

        Result<PriceObservation> observation = PriceObservation.Record(
            offer,
            price,
            collectedOffer.Availability,
            collectedOffer.ObservedOnUtc,
            referencePrices);

        if (observation.IsFailure)
        {
            return false;
        }

        priceObservationRepository.Insert(observation.Value);

        return observation.Value.IsFlaggedAsAnomaly;
    }
}
