using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.ActivateMerchant;

public sealed record ActivateMerchantCommand(Guid MerchantId) : ICommand;
