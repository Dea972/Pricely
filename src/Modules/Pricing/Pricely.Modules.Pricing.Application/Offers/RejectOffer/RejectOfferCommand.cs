using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.RejectOffer;

public sealed record RejectOfferCommand(Guid OfferId) : ICommand;
