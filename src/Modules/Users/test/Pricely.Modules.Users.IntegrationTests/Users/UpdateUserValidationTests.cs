using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Application.Users.UpdateUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using Pricely.Modules.Users.Presentation.Users;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class UpdateUserValidationTests : BaseIntegrationTest
{
    public UpdateUserValidationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        UpdateUserCommand command = new(
            userId,
            Faker.Name.FirstName(),
            Faker.Name.LastName());

        // Act
        Result result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound(userId));
    }

   

    [Fact]
    public async Task Should_UpdateUser_WithCorrectData()
    {
        // Arrange
        Result<Guid> registerResult = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.UserName(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        Guid userId = registerResult.Value;
        string newFirstName = Faker.Name.FirstName();
        string newLastName = Faker.Name.LastName();

        // Act
        Result result = await Sender.Send(new UpdateUserCommand(
            userId,
            newFirstName,
            newLastName));

        // Assert
        result.IsSuccess.Should().BeTrue();

        User? user = await DbContext.Users.FindAsync(userId);
        user.Should().NotBeNull();
        user!.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
    }

    [Fact]
    public async Task Should_NotChangeEmail_WhenUpdatingUser()
    {
        // Arrange
        string originalEmail = Faker.Internet.Email();
        Result<Guid> registerResult = await Sender.Send(new RegisterUserCommand(
            originalEmail,
            Faker.Internet.UserName(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        Guid userId = registerResult.Value;

        // Act
        await Sender.Send(new UpdateUserCommand(
            userId,
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        // Assert
        User? user = await DbContext.Users.FindAsync(userId);
        user!.Email.Should().Be(originalEmail);
    }
}
