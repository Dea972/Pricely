using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Application.Abstractions.Identity;

public static class IdentityProviderErrors
{
    public static readonly Error EmailIsNotUnique = Error.Conflict(
        "Identity.EmailIsNotUnique",
        "The specified email is not unique.");

    public static readonly Error UnauthorizedDomain = Error.Failure(
        "Identity.UnauthorizedDomain",
        "User is not from the authorized domain (Trialis).");

    public static readonly Error RegistrationFailed = Error.Failure(
        "Identity.RegistrationFailed",
        "User registration failed due to an unexpected error.");

    public static readonly Error UnknownUser = Error.NotFound(
        "Identity.UnknownUser",
        "The specified user does not exist in the identity provider.");
}
