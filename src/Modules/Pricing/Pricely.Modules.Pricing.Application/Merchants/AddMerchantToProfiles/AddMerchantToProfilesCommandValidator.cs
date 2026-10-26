using FluentValidation;

namespace Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;

internal sealed class AddMerchantToProfilesCommandValidator : AbstractValidator<AddMerchantToProfilesCommand>
{
    public AddMerchantToProfilesCommandValidator()
    {
        RuleFor(c => c.MerchantId).NotEmpty();
        RuleFor(c => c.ProductCategoryIds).NotEmpty();
    }
}
