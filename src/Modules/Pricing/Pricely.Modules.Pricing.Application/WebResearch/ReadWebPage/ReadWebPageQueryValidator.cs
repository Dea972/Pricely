using FluentValidation;

namespace Pricely.Modules.Pricing.Application.WebResearch.ReadWebPage;

internal sealed class ReadWebPageQueryValidator : AbstractValidator<ReadWebPageQuery>
{
    public ReadWebPageQueryValidator() =>
        RuleFor(q => q.Address)
            .NotNull()
            .Must(a => a is not null && ReadWebPageQueryHandler.IsWebAddress(a) && a.AbsoluteUri.Length <= ReadWebPageQuery.AddressMaxLength)
            .WithMessage("The address must be an absolute http or https address on the default port, without credentials");
}
