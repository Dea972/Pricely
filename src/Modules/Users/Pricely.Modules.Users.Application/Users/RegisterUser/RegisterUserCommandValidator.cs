using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace Pricely.Modules.Users.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.UserName).NotEmpty();
        RuleFor(c => c.Email).EmailAddress();
    }
}
