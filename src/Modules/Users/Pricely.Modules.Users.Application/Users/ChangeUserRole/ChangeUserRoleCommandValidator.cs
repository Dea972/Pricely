using FluentValidation;

namespace Pricely.Modules.Users.Application.Users.ChangeUserRole;

internal sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    private static readonly string[] ValidRoles = ["Administrator", "Member", "Custom"];

    public ChangeUserRoleCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithMessage("L'identifiant de l'utilisateur est requis");

        RuleFor(c => c.RoleName)
            .NotEmpty()
            .WithMessage("Le nom du rôle est requis")
            .Must(role => ValidRoles.Contains(role))
            .WithMessage($"Le rôle doit être l'un des suivants : {string.Join(", ", ValidRoles)}");
    }
}
