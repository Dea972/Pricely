using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.RegisterMerchant;

public sealed record RegisterMerchantCommand(string Name, string Website, string? SearchTemplate = null) : ICommand<Guid>;
