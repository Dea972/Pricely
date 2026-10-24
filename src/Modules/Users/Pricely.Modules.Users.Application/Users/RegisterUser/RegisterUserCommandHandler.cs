using System;
using System.Collections.Generic;
using System.Text;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Abstractions.Data;
using Pricely.Modules.Users.Application.Abstractions.Identity;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IIdentityProviderService identityProviderService)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        Result<Guid> result = await identityProviderService.RegisterUserAsync(
            new UserModel(request.UserName,request.Email,  request.FirstName, request.LastName),
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        return result.Value;
    }
}
