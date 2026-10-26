using FluentValidation;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;

internal sealed class SubmitCollectedOfferCommandValidator : AbstractValidator<SubmitCollectedOfferCommand>
{
    public SubmitCollectedOfferCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Offer).NotNull();
        RuleFor(c => c.Offer.SnapshotId).NotEmpty();
        RuleFor(c => c.Offer.Price).GreaterThan(0);
        RuleFor(c => c.Offer.Currency).NotEmpty().Length(3);
        RuleFor(c => c.Offer.Availability).IsInEnum();
        RuleFor(c => c.Offer.Similarity).InclusiveBetween(0, 1).When(c => c.Offer.Similarity is not null);
        RuleFor(c => c.Offer.ObservedTitle).MaximumLength(MatchEvidence.ObservedTitleMaxLength * 2);
        RuleFor(c => c.Offer.Rationale).MaximumLength(MatchEvidence.AiRationaleMaxLength * 2);
    }
}
