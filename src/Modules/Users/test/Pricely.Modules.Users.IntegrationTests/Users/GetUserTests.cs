using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users;
using Pricely.Modules.Users.Application.Users.GetUser;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class GetUserTests : BaseIntegrationTest
{
    public GetUserTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        Result<UserResponse> userResult = await Sender.Send(new GetUserQuery(userId));

        // Assert
        userResult.Error.Should().Be(UserErrors.NotFound(userId));
    }

    [Fact]
    public async Task Should_ReturnUser_WhenUserExists()
    {
        // Arrange
        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.Password(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));
        Guid userId = result.Value;

        // Act
        Result<UserResponse> userResult = await Sender.Send(new GetUserQuery(userId));

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_ReturnError_WhenUserIdIsEmpty()
    {
        // Act
        Result<UserResponse> userResult = await Sender.Send(new GetUserQuery(Guid.Empty));

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_ReturnCorrectUserProperties()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string userName = Faker.Internet.UserName();
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();

        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            email,
            userName,
            firstName,
            lastName));
        Guid userId = result.Value;

        // Act
        Result<UserResponse> userResult = await Sender.Send(new GetUserQuery(userId));

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.Id.Should().Be(userId);
        userResult.Value.Email.Should().Be(email);
        userResult.Value.FirstName.Should().Be(firstName);
        userResult.Value.LastName.Should().Be(lastName);
    }
}
