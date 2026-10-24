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
    
    // Conversations
    internal const string ConversationsRead = "conversations:read";
    internal const string ConversationsWrite = "conversations:write";
    internal const string ConversationsDelete = "conversations:delete";
    
    // AI Engine
    internal const string AiEngineAccess = "ai:access";
    internal const string AiEngineAdvanced = "ai:advanced";
    
    // Admin
    internal const string AdminAccess = "admin:access";
    internal const string AdminSettings = "admin:settings";
    
    // Legacy (à supprimer si non utilisés)
    internal const string GetUser = UsersRead;
    internal const string ModifyUser = UsersWrite;
}
