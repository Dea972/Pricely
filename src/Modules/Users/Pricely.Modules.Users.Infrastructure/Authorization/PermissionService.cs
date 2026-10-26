using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.GetUserPermissions;
using Pricely.Modules.Users.Application.Users.ProvisionUser;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Infrastructure.Authorization;

internal sealed class PermissionService(ISender sender) : IPermissionService
{
    public async Task<Result<PermissionsResponse>> ResolveCallerAsync(
        IdentityProfile identity,
        CancellationToken cancellationToken = default)
    {
        Result<PermissionsResponse> existing = await sender.Send(new GetUserPermissionsQuery(identity.IdentityId), cancellationToken);

        if (existing.IsSuccess || existing.Error.Type != ErrorType.NotFound)
        {
            return existing;
        }

        // First authenticated request of this identity: create the user (role Member).
        if (string.IsNullOrWhiteSpace(identity.Email))
        {
            return Result.Failure<PermissionsResponse>(UserErrors.MissingEmail);
        }

        try
        {
            Result<Guid> provisioned = await sender.Send(
                new ProvisionUserCommand(
                    identity.IdentityId,
                    identity.Email,
                    identity.FirstName ?? identity.UserName ?? string.Empty,
                    identity.LastName ?? string.Empty),
                cancellationToken);

            if (provisioned.IsFailure)
            {
                return Result.Failure<PermissionsResponse>(provisioned.Error);
            }
        }
        catch (PricelyException exception) when (exception.InnerException is DbUpdateException)
        {
            // A concurrent first request may have created the user (unique identity id): read it back. Any other
            // database failure (e.g. the email already belongs to another identity) is not a race, rethrow it.
            Result<PermissionsResponse> created = await sender.Send(new GetUserPermissionsQuery(identity.IdentityId), cancellationToken);

            if (created.IsFailure)
            {
                throw;
            }

            return created;
        }

        return await sender.Send(new GetUserPermissionsQuery(identity.IdentityId), cancellationToken);
    }
}
