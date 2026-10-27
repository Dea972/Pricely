using FluentValidation;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.WebResearch.ReadWebPage;

internal sealed class ReadWebPageQueryValidator : AbstractValidator<ReadWebPageQuery>
{
    public ReadWebPageQueryValidator() =>
        RuleFor(q => q.Address)
            .NotNull()
            .Must(a => a is not null && ReadWebPageQueryHandler.IsWebAddress(a) && a.AbsoluteUri.Length <= PageSnapshot.AddressMaxLength)
            .WithMessage("The address must be an absolute http or https address");
}
