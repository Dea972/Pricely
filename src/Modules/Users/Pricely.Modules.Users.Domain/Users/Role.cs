using System;
using System.Collections.Generic;
using System.Linq;

namespace Pricely.Modules.Users.Domain.Users;

public sealed class Role
{
    public static readonly Role Administrator = new("Administrator");
    public static readonly Role Member = new("Member");
    public static readonly Role Custom = new("Custom");

    private readonly List<Permission> _permissions = [];

    private Role(string name)
    {
        Name = name;
    }

    private Role()
    {
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<Permission> Permissions => _permissions.ToList();

    public void AddPermission(Permission permission)
    {
        if (_permissions.Any(p => p.Code == permission.Code))
        {
            return;
        }

        _permissions.Add(permission);
    }

    public void RemovePermission(string permissionCode)
    {
        Permission? permission = _permissions.FirstOrDefault(p => p.Code == permissionCode);
        if (permission is not null)
        {
            _permissions.Remove(permission);
        }
    }

    public static Role? FromName(string name)
    {
        return name switch
        {
            "Administrator" => Administrator,
            "Member" => Member,
            "Custom" => Custom,
            _ => null
        };
    }

    public bool IsCustomRole() => Name == "Custom";
}
