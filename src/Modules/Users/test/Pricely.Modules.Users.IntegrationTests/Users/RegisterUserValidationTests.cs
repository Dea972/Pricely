using System.Net;
using System.Net.Http.Json;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using Pricely.Modules.Users.Presentation.Users;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class RegisterUserValidationTests : BaseIntegrationTest
{
    public RegisterUserValidationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Theory]
    [InlineData("", "username", "John", "Doe")]
    [InlineData("invalid-email", "username", "John", "Doe")]
    [InlineData("test@test.com", "", "John", "Doe")]
    [InlineData("test@test.com", "username", "", "Doe")]
    [InlineData("test@test.com", "username", "John", "")]
    public async Task Should_ReturnBadRequest_WhenCommandIsInvalid(
        string email,
        string userName,
        string firstName,
        string lastName)
    {
        // Arrange
        RegisterUser.RegisterRequest request = new()
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
    public async Task Should_CreateUser_WithCorrectData()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string userName = Faker.Internet.UserName();
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();

        // Act
        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            email,
            userName,
            firstName,
            lastName));

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        User? user = await DbContext.Users.FindAsync(result.Value);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
    }

    [Fact]
    public async Task Should_AssignUniqueIdentityId_WhenUserIsCreated()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string userName = Faker.Internet.UserName();

        // Act
        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            email,
            userName,
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        User? user = await DbContext.Users.FindAsync(result.Value);
        user.Should().NotBeNull();
        user!.IdentityId.Should().NotBeNullOrEmpty();
    }
}
