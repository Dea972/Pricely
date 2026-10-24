using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Database;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public sealed class ChangeUserRoleTests : BaseIntegrationTest
{
    public ChangeUserRoleTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ChangeUserRole_ShouldSucceed_WhenRoleIsValid()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var request = new { RoleName = "Administrator" };


        Guid userId = await Sender.RegisterUserAsync();

        // Act
        using HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using IServiceScope scope = ServiceProvider.CreateScope();

        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        // Vérifier en base de données que le rôle a changé
        User user = await dbContext.GetUserByIdAsync(userId);

        user.Should().NotBeNull();

        user!.Roles.Should().HaveCount(1);

        user.Roles.First().Name.Should().Be("Administrator");
    }

    [Fact]
    public async Task ChangeUserRole_ShouldReturnBadRequest_WhenRoleIsInvalid()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");


        Guid userId = await Sender.RegisterUserAsync();

        var request = new { RoleName = "InvalidRole" };

        // Act
        using HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeUserRole_ShouldReturnBadRequest_WhenUserNotFound()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var nonExistentUserId = Guid.NewGuid();
        var request = new { RoleName = "Administrator" };

        // Act
        using HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/users/{nonExistentUserId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeUserRole_ShouldChangeToPreviousRole_WhenCalledMultipleTimes()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        // Changer vers Administrator
        var request1 = new { RoleName = "Administrator" };

        Guid userId = await Sender.RegisterUserAsync();

        await HttpClient.PutAsJsonAsync($"api/users/{userId}/role", request1);

        // Changer vers Member
        var request2 = new { RoleName = "Member" };

        // Act
        using HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"api/users/{userId}/role", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        User? user = await DbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user!.Roles.Should().HaveCount(1);
        user.Roles.First().Name.Should().Be("Member");
    }
}
