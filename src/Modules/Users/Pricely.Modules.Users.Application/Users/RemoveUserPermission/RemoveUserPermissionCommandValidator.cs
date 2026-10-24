using FluentValidation;

namespace Pricely.Modules.Users.Application.Users.RemoveUserPermission;

internal sealed class RemoveUserPermissionCommandValidator : AbstractValidator<RemoveUserPermissionCommand>
{
    public RemoveUserPermissionCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithMessage("L'identifiant de l'utilisateur est requis");

        RuleFor(c => c.PermissionCode)
            .NotEmpty()
            .WithMessage("Le code de permission est requis");
    }
}
