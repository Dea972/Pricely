using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

/// <summary>
/// Collects the merchant offers of a product: plan (typology profile) → collect (port) → match and record.
/// </summary>
internal sealed class CollectProductOffersCommandHandler(
    OfferCollectionPlanner offerCollectionPlanner,
    IOfferCollector offerCollector,
    CollectedOfferRecorder collectedOfferRecorder)
    : ICommandHandler<CollectProductOffersCommand, CollectProductOffersResponse>
{
    public async Task<Result<CollectProductOffersResponse>> Handle(
        CollectProductOffersCommand request,
        CancellationToken cancellationToken)
    {
        Result<OfferCollectionPlan> plan = await offerCollectionPlanner.PlanAsync(request.ProductId, cancellationToken);
        if (plan.IsFailure)
        {
            return Result.Failure<CollectProductOffersResponse>(plan.Error);
        }

        Result<IReadOnlyCollection<CollectedOffer>> collectedOffers =
            await offerCollector.CollectAsync(plan.Value.Request, cancellationToken);
        if (collectedOffers.IsFailure)
        {
            return Result.Failure<CollectProductOffersResponse>(collectedOffers.Error);
        }

        HashSet<Guid> requestedMerchantIds = [.. plan.Value.Request.Merchants.Select(m => m.MerchantId)];
        List<OfferRecording> recordings = [];
        int unrequestedMerchantCount = 0;

        foreach (CollectedOffer collectedOffer in collectedOffers.Value)
        {
            // The collector must stay within the merchants of the typology's profile.
            if (!requestedMerchantIds.Contains(collectedOffer.MerchantId))
            {
                unrequestedMerchantCount++;
                continue;
            }

            recordings.Add(await collectedOfferRecorder.RecordAsync(plan.Value, collectedOffer, cancellationToken));
        }

        return CollectProductOffersResponse.From(collectedOffers.Value.Count, recordings, unrequestedMerchantCount);
    }
}
