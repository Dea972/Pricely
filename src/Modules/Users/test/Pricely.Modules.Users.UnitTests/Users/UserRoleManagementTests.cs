using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.UnitTests.Abstractions;
using FluentAssertions;
using Xunit;

namespace Pricely.Modules.Users.UnitTests.Users;

public sealed class UserRoleManagementTests : BaseTest
{
    [Fact]
    public void ChangeRole_ShouldUpdateRole_WhenCalled()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        // Act
        user.ChangeRole(Role.Administrator);

        // Assert
        user.Roles.Should().HaveCount(1);
        user.Roles.First().Name.Should().Be("Administrator");
    }

    [Fact]
    public void AddUserPermission_ShouldSwitchToCustomRole_WhenUserHasStandardRole()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        // User a le rôle Member par défaut

        // Act
        user.AddUserPermission(Permission.AdminSettings);

        // Assert
        user.Roles.First().Name.Should().Be("Custom");
        user.UserPermissions.Should().Contain(p => p.Code == "admin:settings");
    }

  

    [Fact]
    public void RemoveUserPermission_ShouldWork_OnlyForCustomRole()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        user.AddUserPermission(Permission.AdminSettings); // Passe en Custom

        // Act
        user.RemoveUserPermission("admin:settings");

        // Assert
        user.UserPermissions.Should().NotContain(p => p.Code == "admin:settings");
    }

    [Fact]
    public void RemoveUserPermission_ShouldDoNothing_ForStandardRoles()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        user.ChangeRole(Role.Administrator);

        // Act
        user.RemoveUserPermission("admin:access"); // Devrait ne rien faire

        // Assert
        user.Roles.First().Name.Should().Be("Administrator");
        user.UserPermissions.Should().BeEmpty();
    }

    [Fact]
    public void AddUserPermission_ShouldBeIdempotent()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        // Act
        user.AddUserPermission(Permission.AdminSettings);
        user.AddUserPermission(Permission.AdminSettings);

        // Assert
        user.UserPermissions.Count(p => p.Code == "admin:settings").Should().Be(1);
    }

    [Fact]
    public void ChangeRole_ShouldRaiseUserRoleChangedDomainEvent()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        user.ClearDomainEvents();

        // Act
        user.ChangeRole(Role.Administrator);

        // Assert
        UserRoleChangedDomainEvent? domainEvent = user.DomainEvents.OfType<UserRoleChangedDomainEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.NewRole.Should().Be("Administrator");
    }

    [Fact]
    public void AddUserPermission_ShouldRaiseUserPermissionAddedDomainEvent()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        user.ClearDomainEvents();

        // Act
        user.AddUserPermission(Permission.AdminSettings);

        // Assert
        UserPermissionAddedDomainEvent? domainEvent = user.DomainEvents.OfType<UserPermissionAddedDomainEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.PermissionCode.Should().Be("admin:settings");
    }

    [Fact]
    public void RemoveUserPermission_ShouldRaiseUserPermissionRemovedDomainEvent()
    {
        // Arrange
        var user = User.Create(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Internet.UserName());

        user.AddUserPermission(Permission.AdminSettings);
        user.ClearDomainEvents();

        // Act
        user.RemoveUserPermission("admin:settings");

        // Assert
        UserPermissionRemovedDomainEvent? domainEvent = user.DomainEvents.OfType<UserPermissionRemovedDomainEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.PermissionCode.Should().Be("admin:settings");
    }
}
