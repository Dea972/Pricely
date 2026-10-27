using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;

namespace Pricely.Modules.Pricing.Application.Merchants;

/// <summary>
/// Read side of the merchants (queries), implemented in Infrastructure.
/// </summary>
public interface IMerchantReadStore
{
    Task<IReadOnlyCollection<MerchantResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
