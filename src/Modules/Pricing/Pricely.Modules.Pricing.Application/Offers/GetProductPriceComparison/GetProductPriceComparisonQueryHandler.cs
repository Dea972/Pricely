using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Offers.GetProductPriceComparison;

internal sealed class GetProductPriceComparisonQueryHandler(IOfferReadStore readStore)
    : IQueryHandler<GetProductPriceComparisonQuery, IReadOnlyCollection<MerchantPriceResponse>>
{
    public async Task<Result<IReadOnlyCollection<MerchantPriceResponse>>> Handle(
        GetProductPriceComparisonQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetPriceComparisonAsync(request.ProductId, cancellationToken));
}
