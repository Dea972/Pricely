using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;

/// <summary>
/// The offer must come from a merchant of the product's collection plan and cite a page that shows its price; it is
/// then matched like any collected offer (never confirmed by the collector itself, ADR-009).
/// </summary>
internal sealed class SubmitCollectedOfferCommandHandler(
    OfferCollectionPlanner offerCollectionPlanner,
    SubmittedOfferVerifier submittedOfferVerifier,
    CollectedOfferRecorder collectedOfferRecorder)
    : ICommandHandler<SubmitCollectedOfferCommand, SubmittedOfferResponse>
{
    public async Task<Result<SubmittedOfferResponse>> Handle(SubmitCollectedOfferCommand request, CancellationToken cancellationToken)
    {
        Result<OfferCollectionPlan> plan = await offerCollectionPlanner.PlanAsync(request.ProductId, cancellationToken);
        if (plan.IsFailure)
        {
            return Result.Failure<SubmittedOfferResponse>(plan.Error);
        }

        Result<CollectedOffer> collected = await submittedOfferVerifier.VerifyAsync(request.Offer, cancellationToken);
        if (collected.IsFailure)
        {
            return Result.Failure<SubmittedOfferResponse>(collected.Error);
        }

        if (plan.Value.Request.Merchants.All(m => m.MerchantId != collected.Value.MerchantId))
        {
            return Result.Failure<SubmittedOfferResponse>(OfferErrors.MerchantNotInCollectionPlan(collected.Value.MerchantId, request.ProductId));
        }

        OfferRecording recording = await collectedOfferRecorder.RecordAsync(plan.Value, collected.Value, cancellationToken);

        return new SubmittedOfferResponse(recording.Kind.ToString(), recording.IsFlaggedAsAnomaly);
    }
}
