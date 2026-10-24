using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

public sealed record CollectProductOffersCommand(Guid ProductId) : ICommand<CollectProductOffersResponse>;
