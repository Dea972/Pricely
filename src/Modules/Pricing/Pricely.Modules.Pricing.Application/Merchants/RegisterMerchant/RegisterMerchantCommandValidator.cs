using FluentValidation;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.RegisterMerchant;

internal sealed class RegisterMerchantCommandValidator : AbstractValidator<RegisterMerchantCommand>
{
    public RegisterMerchantCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Merchant.NameMaxLength);
        RuleFor(c => c.Website).NotEmpty();
        RuleFor(c => c.SearchTemplate).MaximumLength(Merchant.SearchTemplateMaxLength);
    }
}
