using FluentValidation;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;

namespace Pricely.Modules.Pricing.Application.CollectionProfiles.DefineCollectionProfile;

internal sealed class DefineCollectionProfileCommandValidator : AbstractValidator<DefineCollectionProfileCommand>
{
    public DefineCollectionProfileCommandValidator()
    {
        RuleFor(c => c.ProductCategoryId).NotEmpty();
        RuleFor(c => c.PreferredMerchantIds).NotNull();
        RuleForEach(c => c.PreferredMerchantIds).NotEmpty();
        RuleFor(c => c.Instructions).MaximumLength(CollectionProfile.InstructionsMaxLength);
    }
}
