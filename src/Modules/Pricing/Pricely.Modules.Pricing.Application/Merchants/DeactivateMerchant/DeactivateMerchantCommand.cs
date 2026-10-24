using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.DeactivateMerchant;

public sealed record DeactivateMerchantCommand(Guid MerchantId) : ICommand;
