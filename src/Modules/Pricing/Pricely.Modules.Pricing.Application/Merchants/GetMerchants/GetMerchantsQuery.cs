using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.GetMerchants;

public sealed record GetMerchantsQuery : IQuery<IReadOnlyCollection<MerchantResponse>>;
