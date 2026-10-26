using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Application.Merchants.GetMerchants;

internal sealed class GetMerchantsQueryHandler(IMerchantReadStore readStore)
    : IQueryHandler<GetMerchantsQuery, IReadOnlyCollection<MerchantResponse>>
{
    public async Task<Result<IReadOnlyCollection<MerchantResponse>>> Handle(
        GetMerchantsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await readStore.GetAllAsync(cancellationToken));
}
