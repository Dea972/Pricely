using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.ProvisionUser;

/// <summary>
/// Creates the user behind an identity (<c>sub</c> of the access token) on its first authenticated request.
/// Idempotent: returns the existing user when there is one.
/// </summary>
public sealed record ProvisionUserCommand(string IdentityId, string Email, string FirstName, string LastName)
    : ICommand<Guid>;
