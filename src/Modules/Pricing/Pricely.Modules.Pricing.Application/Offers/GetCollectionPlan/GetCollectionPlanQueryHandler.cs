using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

namespace Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;

/// <summary>
/// Same plan as a real collection (<see cref="OfferCollectionPlanner"/>), mapped to its own response so that the
/// collector port can evolve without changing this query (and the agent tool built on it).
/// </summary>
internal sealed class GetCollectionPlanQueryHandler(OfferCollectionPlanner offerCollectionPlanner)
    : IQueryHandler<GetCollectionPlanQuery, CollectionPlanResponse>
{
    public async Task<Result<CollectionPlanResponse>> Handle(GetCollectionPlanQuery request, CancellationToken cancellationToken)
    {
        Result<OfferCollectionPlan> plan = await offerCollectionPlanner.PlanAsync(request.ProductId, cancellationToken);
        if (plan.IsFailure)
        {
            return Result.Failure<CollectionPlanResponse>(plan.Error);
        }

        OfferCollectionRequest collection = plan.Value.Request;
        ProductToCollect product = collection.Product;

        return new CollectionPlanResponse(
            new CollectionPlanProductResponse(
                product.ProductId,
                product.Name,
                product.Brand,
                product.Gtin,
                product.ManufacturerReference),
            [.. collection.Merchants.Select(m => new CollectionPlanMerchantResponse(m.MerchantId, m.Name, m.Website, m.Priority, m.SearchTemplate))],
            collection.Instructions);
    }
}
