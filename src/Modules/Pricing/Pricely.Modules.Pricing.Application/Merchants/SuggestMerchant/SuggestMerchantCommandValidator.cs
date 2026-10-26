using FluentValidation;

namespace Pricely.Modules.Pricing.Application.Merchants.SuggestMerchant;

internal sealed class SuggestMerchantCommandValidator : AbstractValidator<SuggestMerchantCommand>
{
    public SuggestMerchantCommandValidator()
    {
        RuleFor(c => c.Website).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Name).MaximumLength(Domain.Merchants.Merchant.NameMaxLength);
    }
}
