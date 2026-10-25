using System;
using System.Collections.Generic;
using System.Text;
using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"The user with the identifier {userId} not found");

    public static Error NotFound(string identityId) =>
        Error.NotFound("Users.NotFound", $"The user with the IDP identifier {identityId} not found");
    
    public static Error NotFoundByEmail(string email) =>
        Error.NotFound("Users.NotFound", $"The user with the email {email} not found");
    
    public static Error NotFoundByUserName(string userName) =>
        Error.NotFound("Users.NotFound", $"The user with the userName {userName} not found");

    public static readonly Error MissingEmail = Error.Problem(
        "Users.MissingEmail",
        "The identity has no email address.");

    public static Error InvalidRole(string roleName) =>
        Error.Problem("Users.InvalidRole", $"The role '{roleName}' is not valid");

    public static Error InvalidPermission(string permissionCode) =>
        Error.Problem("Users.InvalidPermission", $"The permission '{permissionCode}' is not valid");
}
