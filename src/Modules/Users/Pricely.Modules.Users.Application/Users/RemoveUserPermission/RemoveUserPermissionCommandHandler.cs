using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Abstractions.Data;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Application.Users.RemoveUserPermission;

internal sealed class RemoveUserPermissionCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveUserPermissionCommand>
{
    public async Task<Result> Handle(RemoveUserPermissionCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        // Utilise RemoveUserPermission qui ne fonctionne que si rôle Custom
        user.RemoveUserPermission(request.PermissionCode);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
