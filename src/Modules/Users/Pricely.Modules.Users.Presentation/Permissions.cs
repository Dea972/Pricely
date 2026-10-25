namespace Pricely.Modules.Users.Presentation;

/// <summary>
/// Définition centralisée de toutes les permissions de l'application
/// </summary>
internal static class Permissions
{
    // Users
    internal const string UsersRead = "users:read";
    internal const string UsersWrite = "users:write";
    internal const string UsersDelete = "users:delete";
    
    // Admin
    internal const string AdminAccess = "admin:access";
    internal const string AdminSettings = "admin:settings";

}
