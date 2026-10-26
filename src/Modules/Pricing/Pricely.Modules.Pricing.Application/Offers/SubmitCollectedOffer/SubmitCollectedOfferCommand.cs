using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;

/// <summary>Records an offer read on a fetched page for a product: matching, then a price observation.</summary>
public sealed record SubmitCollectedOfferCommand(Guid ProductId, SubmittedOffer Offer) : ICommand<SubmittedOfferResponse>;

/// <param name="Outcome">What happened (<c>NewOfferProposed</c>, <c>NewOfferConfirmed</c>, <c>ExistingOfferObserved</c>...).</param>
/// <param name="IsFlaggedAsAnomaly">The price guard flagged the price.</param>
public sealed record SubmittedOfferResponse(string Outcome, bool IsFlaggedAsAnomaly);
