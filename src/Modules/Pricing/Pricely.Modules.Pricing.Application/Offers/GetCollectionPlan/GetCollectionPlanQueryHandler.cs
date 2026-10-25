using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

namespace Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;

internal sealed class GetCollectionPlanQueryHandler(OfferCollectionPlanner offerCollectionPlanner)
    : IQueryHandler<GetCollectionPlanQuery, OfferCollectionRequest>
{
    public async Task<Result<OfferCollectionRequest>> Handle(GetCollectionPlanQuery request, CancellationToken cancellationToken)
    {
        Result<OfferCollectionPlan> plan = await offerCollectionPlanner.PlanAsync(request.ProductId, cancellationToken);

        return plan.IsSuccess ? plan.Value.Request : Result.Failure<OfferCollectionRequest>(plan.Error);
    }
}
