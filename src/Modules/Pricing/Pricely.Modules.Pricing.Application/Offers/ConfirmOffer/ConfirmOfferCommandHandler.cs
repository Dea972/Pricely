using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.ConfirmOffer;

internal sealed class ConfirmOfferCommandHandler(
    IOfferRepository offerRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmOfferCommand>
{
    public async Task<Result> Handle(ConfirmOfferCommand request, CancellationToken cancellationToken)
    {
        Offer? offer = await offerRepository.GetAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return Result.Failure(OfferErrors.NotFound(request.OfferId));
        }

        Result result = offer.Confirm(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
