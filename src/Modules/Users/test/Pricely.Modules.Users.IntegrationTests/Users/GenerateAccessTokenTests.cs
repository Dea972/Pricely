using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using Pricely.Modules.Users.Presentation.Users;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class GenerateAccessTokenTests : BaseIntegrationTest
{
    public GenerateAccessTokenTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnAccessToken_WhenCredentialIsValid()
    {
        // Arrange
        string userName = Faker.Internet.UserName();
        
        await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            userName,
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        TokenRequest request = new(userName);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/access-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        AccessTokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
        tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_ReturnValidJwtToken_WithCorrectClaims()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string userName = Faker.Internet.UserName();
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();

        await Sender.Send(new RegisterUserCommand(
            email,
            userName,
            firstName,
            lastName));

        TokenRequest request = new(userName);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/access-token", request);
        AccessTokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        // Assert
        tokenResponse.Should().NotBeNull();
        
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = handler.ReadJwtToken(tokenResponse!.AccessToken);
        var claims = token.Claims.ToList();

        claims.Should().Contain(c => c.Type == "email" && c.Value == email);
        claims.Should().Contain(c => c.Type == "given_name" && c.Value == firstName);
        claims.Should().Contain(c => c.Type == "family_name" && c.Value == lastName);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenCredentialIsEmpty()
    {
        // Arrange
        TokenRequest request = new("");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/access-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_WhenUserDoesNotExist()
    {
        // Arrange
        TokenRequest request = new("nonexistent_user");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/access-token", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_GenerateNewToken_OnEachRequest()
    {
        // Arrange
        string userName = Faker.Internet.UserName();
        
        await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            userName,
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        TokenRequest request = new(userName);

        // Act
        HttpResponseMessage response1 = await HttpClient.PostAsJsonAsync("api/users/access-token", request);
        AccessTokenResponse? token1 = await response1.Content.ReadFromJsonAsync<AccessTokenResponse>();

        // Attendre un peu pour s'assurer que les tokens sont différents
        await Task.Delay(100);

        HttpResponseMessage response2 = await HttpClient.PostAsJsonAsync("api/users/access-token", request);
        AccessTokenResponse? token2 = await response2.Content.ReadFromJsonAsync<AccessTokenResponse>();

        // Assert
        token1.Should().NotBeNull();
        token2.Should().NotBeNull();
        token1!.AccessToken.Should().NotBe(token2!.AccessToken);
    }

    [Fact]
    public async Task Should_IncludeUserIdInJwtToken()
    {
        // Arrange
        string userName = Faker.Internet.UserName();
        
        Result<Guid> registerResult = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            userName,
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        User? user = await DbContext.Users.FindAsync(registerResult.Value);
        TokenRequest request = new(userName);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/users/access-token", request);
        AccessTokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        // Assert
        tokenResponse.Should().NotBeNull();
        
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = handler.ReadJwtToken(tokenResponse!.AccessToken);
        var claims = token.Claims.ToList();

        claims.Should().Contain(c => c.Type == "sub" && c.Value == user!.Id.ToString());
    }
}
