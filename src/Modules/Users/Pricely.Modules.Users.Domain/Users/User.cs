using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Domain.Users;

public sealed class User : Entity
{
    private readonly List<Role> _roles = [];
    private readonly List<Permission> _userPermissions = [];

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string IdentityId { get; private set; }

    public IReadOnlyCollection<Role> Roles => _roles.ToList();

    public IReadOnlyCollection<Permission> UserPermissions => _userPermissions.ToList();

    /// <summary>
    /// Retourne toutes les permissions de l'utilisateur :
    /// - Si rôle = Administrator ou Member → Permissions du rôle uniquement
    /// - Si rôle = Custom → Permissions du rôle Custom (vide) + permissions personnalisées
    /// </summary>
    public IReadOnlyCollection<Permission> GetAllPermissions()
    {
        if (_roles.Count == 0)
        {
            return Array.Empty<Permission>();
        }

        Role currentRole = _roles[0];

        // Si rôle standard (Admin ou Member), retourner uniquement les permissions du rôle
        if (!currentRole.IsCustomRole())
        {
            return currentRole.Permissions;
        }

        // Si rôle Custom, combiner permissions du rôle + permissions utilisateur
        return currentRole.Permissions
            .Concat(_userPermissions)
            .DistinctBy(p => p.Code)
            .ToList();
    }

    public static User Create(string email, string firstName, string lastName, string identityId)
    {
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IdentityId = identityId,
        };

        user.AddRole(Role.Member);

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public void Update(string firstName, string lastName)
    {
        if (FirstName == firstName && LastName == lastName)
        {
            return;
        }

        FirstName = firstName;
        LastName = lastName;

        Raise(new UserProfileUpdatedDomainEvent(Id, FirstName, LastName));
    }

    public void AddRole(Role role)
    {
        _roles.Add(role);
    }

    public void ChangeRole(Role newRole)
    {
        _roles.Clear();
        _roles.Add(newRole);

        Raise(new UserRoleChangedDomainEvent(Id, newRole.Name));
    }

    /// <summary>
    /// Ajoute une permission personnalisée à l'utilisateur.
    /// Passe automatiquement l'utilisateur en rôle Custom si nécessaire.
    /// </summary>
    public void AddUserPermission(Permission permission)
    {
        // Si l'utilisateur n'a pas de rôle, ne rien faire
        if (_roles.Count == 0)
        {
            return;
        }

        // Si l'utilisateur a un rôle standard (Admin/Member), passer en Custom
        Role currentRole = _roles[0];
        if (!currentRole.IsCustomRole())
        {
            // Copier les permissions du rôle actuel vers les permissions utilisateur
            foreach (Permission rolePermission in currentRole.Permissions)
            {
                if (!_userPermissions.Any(p => p.Code == rolePermission.Code))
                {
                    _userPermissions.Add(rolePermission);
                }
            }

            // Changer vers le rôle Custom
            ChangeRole(Role.Custom);
        }

        // Ajouter la nouvelle permission si elle n'existe pas déjà
        if (_userPermissions.Any(p => p.Code == permission.Code))
        {
            return;
        }

        _userPermissions.Add(permission);

        Raise(new UserPermissionAddedDomainEvent(Id, permission.Code));
    }

    /// <summary>
    /// Retire une permission personnalisée de l'utilisateur.
    /// Ne fonctionne que si l'utilisateur a le rôle Custom.
    /// </summary>
    public void RemoveUserPermission(string permissionCode)
    {
        if (_roles.Count == 0)
        {
            return;
        }

        // Ne peut retirer des permissions que si rôle Custom
        Role currentRole = _roles[0];
        if (!currentRole.IsCustomRole())
        {
            return;
        }

        Permission? permission = _userPermissions.FirstOrDefault(p => p.Code == permissionCode);
        if (permission is null)
        {
            return;
        }

        _userPermissions.Remove(permission);

        Raise(new UserPermissionRemovedDomainEvent(Id, permissionCode));
    }
}
