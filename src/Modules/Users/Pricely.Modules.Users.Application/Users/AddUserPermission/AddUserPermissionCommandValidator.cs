using FluentValidation;

namespace Pricely.Modules.Users.Application.Users.AddUserPermission;

internal sealed class AddUserPermissionCommandValidator : AbstractValidator<AddUserPermissionCommand>
{
    private static readonly string[] ValidPermissions = 
    [
        "users:read",
        "users:write",
        "users:delete",
        "conversations:read",
        "conversations:write",
        "conversations:delete",
        "ai:access",
        "ai:advanced",
        "admin:access",
        "admin:settings"
    ];

    public AddUserPermissionCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithMessage("L'identifiant de l'utilisateur est requis");

        RuleFor(c => c.PermissionCode)
            .NotEmpty()
            .WithMessage("Le code de permission est requis")
            .Must(code => ValidPermissions.Contains(code))
            .WithMessage($"Le code de permission doit être l'un des suivants : {string.Join(", ", ValidPermissions)}");
    }
}
