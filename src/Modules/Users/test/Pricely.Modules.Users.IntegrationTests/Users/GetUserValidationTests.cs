using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users;
using Pricely.Modules.Users.Application.Users.GetUser;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Application.Users.UpdateUser;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class GetUserValidationTests : BaseIntegrationTest
{
    public GetUserValidationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnUpdatedData_AfterUserUpdate()
    {
        // Arrange
        Result<Guid> registerResult = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.UserName(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        Guid userId = registerResult.Value;
        string newFirstName = "UpdatedFirstName";
        string newLastName = "UpdatedLastName";

        await Sender.Send(new UpdateUserCommand(userId, newFirstName, newLastName));

        // Act
        Result<UserResponse> result = await Sender.Send(new GetUserQuery(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be(newFirstName);
        result.Value.LastName.Should().Be(newLastName);
    }

    [Fact]
    public async Task Should_ReturnCorrectUserData_ForMultipleUsers()
    {
        // Arrange
        string user1Email = Faker.Internet.Email();
        Result<Guid> user1Result = await Sender.Send(new RegisterUserCommand(
            user1Email,
            Faker.Internet.UserName(),
            "User1First",
            "User1Last"));

        string user2Email = Faker.Internet.Email();
        Result<Guid> user2Result = await Sender.Send(new RegisterUserCommand(
            user2Email,
            Faker.Internet.UserName(),
            "User2First",
            "User2Last"));

        // Act
        Result<UserResponse> user1Response = await Sender.Send(new GetUserQuery(user1Result.Value));
        Result<UserResponse> user2Response = await Sender.Send(new GetUserQuery(user2Result.Value));

        // Assert
        user1Response.IsSuccess.Should().BeTrue();
        user1Response.Value.Email.Should().Be(user1Email);
        user1Response.Value.FirstName.Should().Be("User1First");

        user2Response.IsSuccess.Should().BeTrue();
        user2Response.Value.Email.Should().Be(user2Email);
        user2Response.Value.FirstName.Should().Be("User2First");
    }

    [Fact]
    public async Task Should_ReturnAllUserProperties()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();

        Result<Guid> registerResult = await Sender.Send(new RegisterUserCommand(
            email,
            Faker.Internet.UserName(),
            firstName,
            lastName));

        // Act
        Result<UserResponse> result = await Sender.Send(new GetUserQuery(registerResult.Value));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(registerResult.Value);
        result.Value.Email.Should().Be(email);
        result.Value.FirstName.Should().Be(firstName);
        result.Value.LastName.Should().Be(lastName);
    }
}
