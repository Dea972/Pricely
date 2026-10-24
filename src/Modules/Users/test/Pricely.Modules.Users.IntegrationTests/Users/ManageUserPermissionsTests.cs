using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Azure.Core;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Database;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Pricely.Modules.Users.Presentation.Users.AddUserPermission;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public sealed class ManageUserPermissionsTests : BaseIntegrationTest
{
    public ManageUserPermissionsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AddUserPermission_ShouldSucceed_WhenPermissionIsValid()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        Guid userId = await Sender.RegisterUserAsync();

        var request = new AddUserPermissionRequest("admin:settings");

        // Act
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Vérifier en base de données
        User user = await DbContext.GetUserByIdAsync(userId);
        user.Should().NotBeNull();
        user!.UserPermissions.Should().Contain(p => p.Code == "admin:settings");
    }

    [Fact]
    public async Task AddUserPermission_ShouldReturnBadRequest_WhenPermissionIsInvalid()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        Guid userId = await Sender.RegisterUserAsync();

        var request = new AddUserPermissionRequest("invalid:permission");

        // Act
        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveUserPermission_ShouldSucceed_WhenPermissionExists()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        // Ajouter d'abord une permission
        var addRequest = new AddUserPermissionRequest("admin:settings");


        Guid userId = await Sender.RegisterUserAsync();

        await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", addRequest);

        // Act
        using HttpResponseMessage response = await HttpClient.DeleteAsync($"api/users/{userId}/permissions/admin:settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        User user = await DbContext.GetUserByIdAsync(userId);

        user!.Roles.First().Permissions.Should().NotContain(p => p.Code == "admin:settings");
    }

    [Fact]
    public async Task AddUserPermission_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        await CleanDatabaseAsync();

        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");       

        Guid userId = await Sender.RegisterUserAsync();

        var request = new AddUserPermissionRequest("admin:settings");

        // Act - Ajouter deux fois la même permission
        await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", request);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        User user = await DbContext.GetUserByIdAsync(userId);

        // La permission ne doit être présente qu'une seule fois
        user!.UserPermissions.Count(p => p.Code == "admin:settings").Should().Be(1);
    }

    [Fact]
    public async Task ManagePermissions_ShouldWorkWithMultiplePermissions()
    {
        // Arrange
        await CleanDatabaseAsync();


        string accessToken = await GetAdminAccessTokenAsync();

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        Guid userId = await Sender.RegisterUserAsync();

        // Act - Ajouter plusieurs permissions
        await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", new { PermissionCode = "admin:settings" });
        await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", new { PermissionCode = "admin:access" });
        await HttpClient.PostAsJsonAsync($"api/users/{userId}/permissions", new { PermissionCode = "users:write" });

        // Assert
        User user = await DbContext.GetUserByIdAsync(userId);

        user!.UserPermissions.Should().HaveCount(3);

        user.UserPermissions.Select(p => p.Code).Should().Contain(
        [
            "admin:settings",
            "admin:access",
            "users:write"
        ]);
    }
}
