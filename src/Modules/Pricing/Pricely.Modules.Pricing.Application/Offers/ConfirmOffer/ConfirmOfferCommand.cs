using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.ConfirmOffer;

public sealed record ConfirmOfferCommand(Guid OfferId) : ICommand;
