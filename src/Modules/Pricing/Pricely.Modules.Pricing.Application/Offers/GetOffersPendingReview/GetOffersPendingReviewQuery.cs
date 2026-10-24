using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;

/// <summary>
/// Offers proposed by the collector that wait for a human review, oldest first.
/// </summary>
public sealed record GetOffersPendingReviewQuery : IQuery<IReadOnlyCollection<PendingOfferResponse>>;
