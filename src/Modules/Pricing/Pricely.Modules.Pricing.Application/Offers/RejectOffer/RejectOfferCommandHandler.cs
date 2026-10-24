using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Data;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.RejectOffer;

internal sealed class RejectOfferCommandHandler(
    IOfferRepository offerRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RejectOfferCommand>
{
    public async Task<Result> Handle(RejectOfferCommand request, CancellationToken cancellationToken)
    {
        Offer? offer = await offerRepository.GetAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return Result.Failure(OfferErrors.NotFound(request.OfferId));
        }

        Result result = offer.Reject(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
