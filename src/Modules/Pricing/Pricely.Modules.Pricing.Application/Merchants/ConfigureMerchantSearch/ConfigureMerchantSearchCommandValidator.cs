using FluentValidation;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.ConfigureMerchantSearch;

internal sealed class ConfigureMerchantSearchCommandValidator : AbstractValidator<ConfigureMerchantSearchCommand>
{
    public ConfigureMerchantSearchCommandValidator()
    {
        RuleFor(c => c.MerchantId).NotEmpty();
        RuleFor(c => c.SearchTemplate).MaximumLength(Merchant.SearchTemplateMaxLength);
    }
}
