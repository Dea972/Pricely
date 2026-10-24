using System;
using System.Collections.Generic;
using System.Text;

namespace Pricely.Modules.Users.Domain.Users;

public sealed class Permission
{
    // ===== USERS =====
    public static readonly Permission UsersRead = new("users:read");
    public static readonly Permission UsersWrite = new("users:write");
    public static readonly Permission UsersDelete = new("users:delete");
    
    // ===== CONVERSATIONS =====
    public static readonly Permission ConversationsRead = new("conversations:read");
    public static readonly Permission ConversationsWrite = new("conversations:write");
    public static readonly Permission ConversationsDelete = new("conversations:delete");
    
    // ===== AI ENGINE =====
    public static readonly Permission AiAccess = new("ai:access");
    public static readonly Permission AiAdvanced = new("ai:advanced");
    
    // ===== ADMIN =====
    public static readonly Permission AdminAccess = new("admin:access");
    public static readonly Permission AdminSettings = new("admin:settings");
    
    // ===== LEGACY (Backward compatibility) =====
    public static readonly Permission GetUser = UsersRead;
    public static readonly Permission ModifyUser = UsersWrite;
    public static readonly Permission Talk = ConversationsRead;
    public static readonly Permission ModifyChatbotSetting = ConversationsWrite;

    private static readonly Dictionary<string, Permission> AllPermissions = new()
    {
        ["users:read"] = UsersRead,
        ["users:write"] = UsersWrite,
        ["users:delete"] = UsersDelete,
        ["conversations:read"] = ConversationsRead,
        ["conversations:write"] = ConversationsWrite,
        ["conversations:delete"] = ConversationsDelete,
        ["ai:access"] = AiAccess,
        ["ai:advanced"] = AiAdvanced,
        ["admin:access"] = AdminAccess,
        ["admin:settings"] = AdminSettings
    };

    public Permission(string code)
    {
        Code = code;
    }

    public string Code { get; }
    
    public string Name { get; set; } = string.Empty;

    public static Permission? FromCode(string code)
    {
        return AllPermissions.TryGetValue(code, out Permission? permission) ? permission : null;
    }

    public static IEnumerable<Permission> GetAll()
    {
        return AllPermissions.Values;
    }
}
