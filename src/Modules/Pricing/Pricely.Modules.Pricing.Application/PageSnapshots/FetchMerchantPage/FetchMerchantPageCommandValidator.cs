using FluentValidation;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.PageSnapshots.FetchMerchantPage;

internal sealed class FetchMerchantPageCommandValidator : AbstractValidator<FetchMerchantPageCommand>
{
    public FetchMerchantPageCommandValidator()
    {
        RuleFor(c => c.MerchantId).NotEmpty();
        RuleFor(c => c)
            .Must(c => c.Address is not null || !string.IsNullOrWhiteSpace(c.SearchText))
            .WithMessage("Give the address of a page or a search text");
        RuleFor(c => c.Address)
            .Must(a => a is null || a.IsAbsoluteUri && a.AbsoluteUri.Length <= PageSnapshot.AddressMaxLength)
            .WithMessage("The address must be absolute");
        RuleFor(c => c.SearchText).MaximumLength(200);
    }
}
