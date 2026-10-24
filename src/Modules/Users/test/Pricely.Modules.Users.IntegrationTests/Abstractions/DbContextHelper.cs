using System;
using System.Threading;
using System.Threading.Tasks;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Database;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Pricely.Modules.Users.IntegrationTests.Abstractions;

/// <summary>
/// DB helper for integration tests — create users with specific roles/permissions quickly.
/// Use the <see cref="UsersDbContext"/> instance from the test scope.
/// </summary>
internal static class DbContextHelper
{
    /// <summary>
    /// Create and persist an admin user. IdentityId defaults to the provided identityId or the email when null.
    /// Ensures the Administrator role from the database is used (avoids duplicate role inserts).
    /// </summary>
    public static async Task<User> CreateAdminUserAsync(
        this UsersDbContext dbContext,
        string email,
        string firstName,
        string lastName,
        string? identityId = null,
        CancellationToken cancellationToken = default)
    {
        identityId ??= email;

        // Ensure Administrator role exists in DB and is tracked
        Role? adminRole = await dbContext
            .Set<Role>()
            .FindAsync([Role.Administrator.Name], cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Administrator role not found in database. Ensure roles are seeded for tests.");

        // Create the domain user (this will add the default Member role)
        var user = User.Create(email, firstName, lastName, identityId);

        // Replace role(s) with the tracked admin role
        user.ChangeRole(adminRole);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user;
    }

    /// <summary>
    /// Create and persist an admin user and attach additional explicit permissions to the user's active role.
    /// Permissions must exist in DB (seeded); this method fetches permission entities to avoid duplicate inserts.
    /// </summary>
    public static async Task<User> CreateAdminUserWithPermissionsAsync(
        this UsersDbContext dbContext,
        string email,
        string firstName,
        string lastName,
        string[] permissionCodes,
        string? identityId = null,
        CancellationToken cancellationToken = default)
    {
        identityId ??= email;

        Role? adminRole = await dbContext
            .Set<Role>()
            .FindAsync([Role.Administrator.Name], cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Administrator role not found in database. Ensure roles are seeded for tests.");

        var user = User.Create(email, firstName, lastName, identityId);
        user.ChangeRole(adminRole);

        // Attach permissions (fetch tracked Permission entities)
        foreach (string code in permissionCodes)
        {
            Permission? permissionEntity = await dbContext
                .Set<Permission>()
                .FindAsync([code], cancellationToken)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"Permission '{code}' not found in database. Ensure permissions are seeded for tests.");

            user.AddUserPermission(permissionEntity);
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user;
    }

    public static async Task<User> GetUserByIdAsync(this UsersDbContext dbContext, Guid userId)
    {

        User? user = await dbContext.Users
            .Include(u => u.Roles)
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user.Should().NotBeNull();

        return user;
    }
}
