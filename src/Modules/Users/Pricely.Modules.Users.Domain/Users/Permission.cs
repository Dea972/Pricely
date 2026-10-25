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
    
    // ===== ADMIN =====
    public static readonly Permission AdminAccess = new("admin:access");
    public static readonly Permission AdminSettings = new("admin:settings");
    

    private static readonly Dictionary<string, Permission> AllPermissions = new()
    {
        ["users:read"] = UsersRead,
        ["users:write"] = UsersWrite,
        ["users:delete"] = UsersDelete,
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
