using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using Pricely.Modules.Users.Presentation.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json.Linq;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class RegisterUserTests : BaseIntegrationTest
{
    public RegisterUserTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    public static readonly TheoryData<string, string, string, string> InvalidRequests = new()
    {
        { "", Faker.Internet.UserName(), Faker.Name.FirstName(), Faker.Name.LastName() },
        { Faker.Internet.Email(), "", Faker.Name.FirstName(), Faker.Name.LastName() },
        { Faker.Internet.Email(), Faker.Internet.UserName(), "", Faker.Name.LastName() },
        { Faker.Internet.Email(), Faker.Internet.UserName(), Faker.Name.FirstName(), "" }
    };


    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Should_ReturnBadRequest_WhenRequestIsNotValid(
        string email,
        string userName,
        string firstName,
        string lastName)
    {
        // Arrange
        var request = new RegisterUser.RegisterRequest
        {
            Email = email,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterUser.RegisterRequest
        {
            Email = "create@test.com",
            UserName = "trialis/egado",
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName()
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_ReturnAccessToken_WhenUserIsRegistered()
    {
        // Arrange
        var request = new RegisterUser.RegisterRequest
        {
            Email = Faker.Internet.Email(),
            UserName = Faker.Internet.UserName(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName()
        };

        await HttpClient.PostAsJsonAsync("api/users/register", request);

        // Act
        string accessToken = await GetAccessTokenAsync(request.Email, request.UserName);

        // Assert
        accessToken.Should().NotBeEmpty();

        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken? token;

        token = handler.ReadJwtToken(accessToken);

        var claims = token.Claims.ToList();

        claims.Should().Contain(c => c.Type == "email" && c.Value == request.Email);
        claims.Should().Contain(c => c.Type == "winaccountname" && c.Value == $"trialis\\{request.UserName}");
        claims.Should().Contain(c => c.Type == "nameid" && c.Value == request.UserName);
        claims.Should().Contain(c => c.Type == "given_name" && c.Value == request.FirstName);
        claims.Should().Contain(c => c.Type == "family_name" && c.Value == request.LastName);
        claims.Should().Contain(c => c.Type == "unique_name" && c.Value == $"{request.FirstName} {request.LastName.ToUpperInvariant()}");

    }

    [Fact]
    public async Task Should_ReturnUserId_WhenUserIsRegistered()
    {
        // Arrange
        var request = new RegisterUser.RegisterRequest
        {
            Email = Faker.Internet.Email(),
            UserName = Faker.Internet.UserName(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName()
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        string content = await response.Content.ReadAsStringAsync();
        var userId = Guid.Parse(content.Trim('"'));
        
        userId.Should().NotBeEmpty();

        User? user = await DbContext.Users.FindAsync(userId);
        user.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_CreateUserInDatabase_WhenRegistered()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string userName = Faker.Internet.UserName();
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();

        var request = new RegisterUser.RegisterRequest
        {
            Email = email,
            UserName = userName,
            FirstName = firstName,
            LastName = lastName
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/register", request);
        string content = await response.Content.ReadAsStringAsync();
        var userId = Guid.Parse(content.Trim('"'));

        // Assert
        User? user = await DbContext.Users.FindAsync(userId);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        string email = Faker.Internet.Email();
        
        var request1 = new RegisterUser.RegisterRequest
        {
            Email = email,
            UserName = Faker.Internet.UserName(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName()
        };

        var request2 = new RegisterUser.RegisterRequest
        {
            Email = email, // Même email
            UserName = Faker.Internet.UserName(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName()
        };

        await HttpClient.PostAsJsonAsync("api/users/register", request1);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
