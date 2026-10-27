using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantProductPage;

/// <summary>
/// Sets (or removes, when empty) the product page of the merchant's site reached by the GTIN, that the collection opens
/// first (ADR-026).
/// </summary>
public sealed record ConfigureMerchantProductPageCommand(Guid MerchantId, string? ProductPageTemplate) : ICommand;
