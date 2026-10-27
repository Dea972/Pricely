using FluentValidation;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantProductPage;

internal sealed class ConfigureMerchantProductPageCommandValidator : AbstractValidator<ConfigureMerchantProductPageCommand>
{
    public ConfigureMerchantProductPageCommandValidator()
    {
        RuleFor(c => c.MerchantId).NotEmpty();
        RuleFor(c => c.ProductPageTemplate).MaximumLength(Merchant.ProductPageTemplateMaxLength);
    }
}
