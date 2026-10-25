namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

public enum OfferRecordingKind
{
    /// <summary>New offer waiting for a review.</summary>
    NewOfferProposed = 0,

    /// <summary>New offer confirmed automatically (GTIN or brand + manufacturer reference).</summary>
    NewOfferConfirmed = 1,

    /// <summary>Known offer: a new price observation was recorded.</summary>
    ExistingOfferObserved = 2,

    /// <summary>The offer was rejected in a previous review.</summary>
    IgnoredRejectedOffer = 3,

    /// <summary>The collector returned an unusable price.</summary>
    IgnoredInvalidPrice = 4
}

public sealed record OfferRecording(OfferRecordingKind Kind, bool IsFlaggedAsAnomaly);
