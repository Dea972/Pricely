using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Abstractions.Data;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Application.Users.ProvisionUser;

internal sealed class ProvisionUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ProvisionUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ProvisionUserCommand request, CancellationToken cancellationToken)
    {
        User? existing = await userRepository.GetAsync(request.IdentityId, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var user = User.Create(request.Email, request.FirstName, request.LastName, request.IdentityId);

        userRepository.Insert(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
