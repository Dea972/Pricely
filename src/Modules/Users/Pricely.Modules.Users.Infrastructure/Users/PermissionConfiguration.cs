using Pricely.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pricely.Modules.Users.Infrastructure.Users;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Code);

        builder.Property(p => p.Code).HasMaxLength(100);

        builder.Property(p => p.Name).HasMaxLength(200);

        // Seed des permissions correspondant au script 01_InitializePermissions.sql
        builder.HasData(
            // Users permissions
            CreatePermission(Permission.UsersRead, "Read Users"),
            CreatePermission(Permission.UsersWrite, "Write Users"),
            CreatePermission(Permission.UsersDelete, "Delete Users"),
            
            // Conversations permissions
            CreatePermission(Permission.ConversationsRead, "Read Conversations"),
            CreatePermission(Permission.ConversationsWrite, "Write Conversations"),
            CreatePermission(Permission.ConversationsDelete, "Delete Conversations"),
            
            // AI Engine permissions
            CreatePermission(Permission.AiAccess, "AI Access"),
            CreatePermission(Permission.AiAdvanced, "AI Advanced"),
            
            // Admin permissions
            CreatePermission(Permission.AdminAccess, "Admin Access"),
            CreatePermission(Permission.AdminSettings, "Admin Settings")
        );

        builder
            .HasMany<Role>()
            .WithMany()
            .UsingEntity(joinBuilder =>
            {
                joinBuilder.ToTable("role_permissions");

                joinBuilder.HasData(
                    // ===== MEMBER ROLE PERMISSIONS =====
                    CreateRolePermission(Role.Member, Permission.UsersRead),
                    CreateRolePermission(Role.Member, Permission.ConversationsRead),
                    CreateRolePermission(Role.Member, Permission.ConversationsWrite),
                    CreateRolePermission(Role.Member, Permission.AiAccess),
                    
                    // ===== ADMINISTRATOR ROLE PERMISSIONS (toutes les permissions) =====
                    CreateRolePermission(Role.Administrator, Permission.UsersRead),
                    CreateRolePermission(Role.Administrator, Permission.UsersWrite),
                    CreateRolePermission(Role.Administrator, Permission.UsersDelete),
                    CreateRolePermission(Role.Administrator, Permission.ConversationsRead),
                    CreateRolePermission(Role.Administrator, Permission.ConversationsWrite),
                    CreateRolePermission(Role.Administrator, Permission.ConversationsDelete),
                    CreateRolePermission(Role.Administrator, Permission.AiAccess),
                    CreateRolePermission(Role.Administrator, Permission.AiAdvanced),
                    CreateRolePermission(Role.Administrator, Permission.AdminAccess),
                    CreateRolePermission(Role.Administrator, Permission.AdminSettings)
                );
            });
    }

    private static object CreatePermission(Permission permission, string name)
    {
        return new
        {
            permission.Code,
            Name = name
        };
    }

    private static object CreateRolePermission(Role role, Permission permission)
    {
        return new
        {
            RoleName = role.Name,
            PermissionCode = permission.Code
        };
    }
}
