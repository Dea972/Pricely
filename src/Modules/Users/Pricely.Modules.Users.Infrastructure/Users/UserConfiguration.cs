using Pricely.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pricely.Modules.Users.Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(300);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.FirstName).HasMaxLength(200);

        builder.Property(u => u.LastName).HasMaxLength(200);

        builder.Property(u => u.IdentityId).HasMaxLength(256);
        builder.HasIndex(u => u.IdentityId).IsUnique();

        // CORRIGÉ : Relation Many-to-Many avec Permission pour les permissions utilisateur
        builder
            .HasMany(u => u.UserPermissions)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_permissions",
                j => j
                    .HasOne<Permission>()
                    .WithMany()
                    .HasForeignKey("permission_code")
                    .HasPrincipalKey(p => p.Code),
                j => j
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("user_id")
                    .HasPrincipalKey(u => u.Id),
                j => j.HasKey("user_id", "permission_code"));
    }
}
