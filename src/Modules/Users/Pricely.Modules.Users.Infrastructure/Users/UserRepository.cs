using System;
using System.Collections.Generic;
using System.Text;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Pricely.Modules.Users.Infrastructure.Users;

internal sealed class UserRepository(UsersDbContext context) 
    : IUserRepository
{
    public async Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.UserPermissions) // NOUVEAU : Inclure les permissions utilisateur
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetAsync(id, cancellationToken);
    }

    public async Task<User?> GetAsync(string identityId, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.UserPermissions) // NOUVEAU : Inclure les permissions utilisateur
            .FirstOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }

    public void Insert(User user)
    {
        foreach (Role role in user.Roles)
        {
            context.Attach(role);
        }

        context.Users.Add(user);
    }

    public void UntrackStaticEntities()
    {
        var modifiedUsers = context.ChangeTracker
            .Entries<User>()
            .Select(e => e.Entity)
            .ToList();

        foreach (User user in modifiedUsers)
        {
            // Détacher les Roles
            foreach (Role role in user.Roles.ToList())
            {
                EntityEntry<Role> entry = context.Entry(role);
                if (entry.State == EntityState.Added)
                {
                    entry.State = EntityState.Unchanged; // ✅ Détache l'instance statique
                }
            }

            // Détacher les Permissions
            foreach (Permission permission in user.UserPermissions.ToList())
            {
                EntityEntry<Permission> entry = context.Entry(permission);
                if (entry.State == EntityState.Added)
                {
                    entry.State = EntityState.Unchanged; // ✅ Détache l'instance statique
                }
            }
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .Include(u => u.UserPermissions) // NOUVEAU : Inclure les permissions utilisateur
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}
