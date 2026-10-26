using FluentAssertions;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.UnitTests.Users;

public class PermissionTests
{
    [Fact]
    public void GetAll_ShouldOnlyContainTheUsersAndAdminPermissions()
    {
        Permission.GetAll().Select(p => p.Code).Should().BeEquivalentTo(
            "users:read",
            "users:write",
            "users:delete",
            "admin:access",
            "admin:settings");
    }

    [Theory]
    [InlineData("conversations:read")]
    [InlineData("ai:access")]
    public void FromCode_ShouldReturnNull_ForRemovedLegacyPermissions(string code)
    {
        Permission.FromCode(code).Should().BeNull();
    }
}
