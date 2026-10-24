using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Application.Abstractions.Identity;

public interface IIdentityProviderService
{
    Task<Result<string>> GenerateAccessTokenAsync(string credential, CancellationToken cancellationToken = default);

    Task<string> RefreshAccessTokenAsync(string identityId);

    Task<Result<Guid>> RegisterUserAsync(UserModel user, CancellationToken cancellationToken = default);
}
