using FluentValidation;

namespace Pricely.Modules.Catalog.Application.Products.SuggestProduct;

internal sealed class SuggestProductCommandValidator : AbstractValidator<SuggestProductCommand>
{
    public SuggestProductCommandValidator() =>
        RuleFor(c => c.ProductPage)
            .NotNull()
            .Must(page => page is { IsAbsoluteUri: true, Scheme: "http" or "https" })
            .WithMessage("The product page must be an absolute http(s) address.");
}
