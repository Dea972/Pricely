using FluentValidation;

namespace Pricely.Modules.Users.Application.Users.ProvisionUser;

internal sealed class ProvisionUserCommandValidator : AbstractValidator<ProvisionUserCommand>
{
    public ProvisionUserCommandValidator()
    {
        RuleFor(c => c.IdentityId).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(300);
        RuleFor(c => c.FirstName).MaximumLength(200);
        RuleFor(c => c.LastName).MaximumLength(200);
    }
}
