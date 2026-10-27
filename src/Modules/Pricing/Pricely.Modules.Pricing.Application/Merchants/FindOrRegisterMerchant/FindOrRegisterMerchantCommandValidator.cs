using FluentValidation;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.Merchants.FindOrRegisterMerchant;

internal sealed class FindOrRegisterMerchantCommandValidator : AbstractValidator<FindOrRegisterMerchantCommand>
{
    public FindOrRegisterMerchantCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(Merchant.NameMaxLength);
        RuleFor(c => c.Website)
            .NotNull()
            .Must(w => w is { IsAbsoluteUri: true } && w.Scheme is "http" or "https")
            .WithMessage("The website must be an absolute http or https address");
    }
}
