namespace Pricely.Modules.Pricing.Application.Merchants.GetMerchants;

public sealed record MerchantResponse(Guid Id, string Name, string Website, bool IsActive);
