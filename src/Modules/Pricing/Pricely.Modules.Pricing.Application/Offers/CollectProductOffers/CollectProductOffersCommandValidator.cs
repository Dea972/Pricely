using FluentValidation;

namespace Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;

internal sealed class CollectProductOffersCommandValidator : AbstractValidator<CollectProductOffersCommand>
{
    public CollectProductOffersCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
    }
}
