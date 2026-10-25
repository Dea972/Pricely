using FluentValidation;

namespace Pricely.Modules.Pricing.Application.Offers.GetPriceAnomalies;

internal sealed class GetPriceAnomaliesQueryValidator : AbstractValidator<GetPriceAnomaliesQuery>
{
    public GetPriceAnomaliesQueryValidator()
    {
        RuleFor(q => q.Limit).InclusiveBetween(1, GetPriceAnomaliesQuery.MaxLimit);
    }
}
