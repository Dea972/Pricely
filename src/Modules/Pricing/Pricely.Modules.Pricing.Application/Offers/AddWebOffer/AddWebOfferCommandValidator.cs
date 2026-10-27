using FluentValidation;

namespace Pricely.Modules.Pricing.Application.Offers.AddWebOffer;

internal sealed class AddWebOfferCommandValidator : AbstractValidator<AddWebOfferCommand>
{
    public AddWebOfferCommandValidator()
    {
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.ProductGtin)
                || !string.IsNullOrWhiteSpace(c.ProductBrand) && !string.IsNullOrWhiteSpace(c.ProductReference))
            .WithMessage("Identify the product by its GTIN, or by its brand and manufacturer reference");
        RuleFor(c => c.PageAddress)
            .NotNull()
            .Must(a => a is { IsAbsoluteUri: true } && a.Scheme is "http" or "https")
            .WithMessage("The page address must be an absolute http or https address");
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
    }
}
