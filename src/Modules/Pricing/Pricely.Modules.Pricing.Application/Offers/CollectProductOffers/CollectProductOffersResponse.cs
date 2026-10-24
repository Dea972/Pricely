namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

/// <param name="CollectedCount">Offers returned by the collector.</param>
/// <param name="ProposedCount">New offers waiting for a review.</param>
/// <param name="AutoConfirmedCount">New offers confirmed automatically.</param>
/// <param name="ObservedCount">Known offers that received a new price.</param>
/// <param name="IgnoredCount">Offers ignored (rejected offer, unusable price, merchant not requested).</param>
/// <param name="AnomalyCount">Prices flagged as anomalous.</param>
public sealed record CollectProductOffersResponse(
    int CollectedCount,
    int ProposedCount,
    int AutoConfirmedCount,
    int ObservedCount,
    int IgnoredCount,
    int AnomalyCount)
{
    internal static CollectProductOffersResponse From(
        int collectedCount,
        IReadOnlyCollection<OfferRecording> recordings,
        int unrequestedMerchantCount) =>
        new(
            collectedCount,
            recordings.Count(r => r.Kind == OfferRecordingKind.NewOfferProposed),
            recordings.Count(r => r.Kind == OfferRecordingKind.NewOfferConfirmed),
            recordings.Count(r => r.Kind == OfferRecordingKind.ExistingOfferObserved),
            unrequestedMerchantCount + recordings.Count(r =>
                r.Kind is OfferRecordingKind.IgnoredRejectedOffer or OfferRecordingKind.IgnoredInvalidPrice),
            recordings.Count(r => r.IsFlaggedAsAnomaly));
}
