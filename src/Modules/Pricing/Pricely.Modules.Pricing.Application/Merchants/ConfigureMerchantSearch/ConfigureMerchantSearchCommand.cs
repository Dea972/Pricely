using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantSearch;

/// <summary>Sets (or removes, when empty) the search page of the merchant's site used by the collection agent.</summary>
public sealed record ConfigureMerchantSearchCommand(Guid MerchantId, string? SearchTemplate) : ICommand;
