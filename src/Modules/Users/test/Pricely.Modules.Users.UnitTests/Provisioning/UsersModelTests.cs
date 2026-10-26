using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Database;

namespace Pricely.Modules.Users.UnitTests.Provisioning;

public class UsersModelTests
{
    [Fact]
    public void RolePermissions_ShouldBeLoadedThroughTheRolePermissionsJoinTable_AndNotThroughAColumnOfPermissions()
    {
        using var context = new UsersDbContext(
            new DbContextOptionsBuilder<UsersDbContext>().UseNpgsql("Host=localhost").Options);

        IEntityType role = context.Model.FindEntityType(typeof(Role))!;
        IEntityType permission = context.Model.FindEntityType(typeof(Permission))!;

        role.GetSkipNavigations().Should().ContainSingle(n => n.Name == nameof(Role.Permissions))
            .Which.JoinEntityType.GetTableName().Should().Be("role_permissions");
        permission.GetForeignKeys().Should().BeEmpty();
    }
}
