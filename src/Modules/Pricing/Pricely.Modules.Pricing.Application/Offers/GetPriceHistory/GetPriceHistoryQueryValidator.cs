using FluentValidation;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceHistory;

internal sealed class GetPriceHistoryQueryValidator : AbstractValidator<GetPriceHistoryQuery>
{
    public GetPriceHistoryQueryValidator()
    {
        RuleFor(q => q.ProductId).NotEmpty();
        RuleFor(q => q.Limit).InclusiveBetween(1, GetPriceHistoryQuery.MaxLimit);
    }
}
