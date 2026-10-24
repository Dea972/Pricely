using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Application.Users.UpdateUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class UpdateUserTests : BaseIntegrationTest
{
    public UpdateUserTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    public static readonly TheoryData<UpdateUserCommand> InvalidCommands = new()
    {
        new UpdateUserCommand(Guid.Empty, Faker.Name.FirstName(), Faker.Name.LastName()),
        new UpdateUserCommand(Guid.NewGuid(), "", Faker.Name.LastName()),
        new UpdateUserCommand(Guid.NewGuid(), Faker.Name.FirstName(), "")
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Should_ReturnError_WhenCommandIsNotValid(UpdateUserCommand command)
    {
        // Act
        Result result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Should_ReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        Result updateResult = await Sender.Send(
            new UpdateUserCommand(userId, Faker.Name.FirstName(), Faker.Name.LastName()));

        // Assert
        updateResult.Error.Should().Be(UserErrors.NotFound(userId));
    }

    [Fact]
    public async Task Should_ReturnSuccess_WhenUserExists()
    {
        // Arrange
        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.Password(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        Guid userId = result.Value;

        // Act
        Result updateResult = await Sender.Send(
            new UpdateUserCommand(userId, Faker.Name.FirstName(), Faker.Name.LastName()));

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Should_UpdateOnlyFirstNameAndLastName()
    {
        // Arrange
        string originalEmail = Faker.Internet.Email();
        string originalUserName = Faker.Internet.UserName();
        
        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            originalEmail,
            originalUserName,
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        Guid userId = result.Value;
        string newFirstName = "NewFirstName";
        string newLastName = "NewLastName";

        // Act
        await Sender.Send(new UpdateUserCommand(userId, newFirstName, newLastName));

        // Assert
        User? user = await DbContext.Users.FindAsync(userId);
        user.Should().NotBeNull();
        user!.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
        user.Email.Should().Be(originalEmail); // Email ne doit pas changer
    }

    [Fact]
    public async Task Should_PersistChanges_AfterUpdate()
    {
        // Arrange
        Result<Guid> result = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.UserName(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        Guid userId = result.Value;
        string newFirstName = "PersistentFirstName";
        string newLastName = "PersistentLastName";

        // Act
        await Sender.Send(new UpdateUserCommand(userId, newFirstName, newLastName));

        // Recharger depuis la DB pour vérifier la persistance
        DbContext.ChangeTracker.Clear();
        User? user = await DbContext.Users.FindAsync(userId);

        // Assert
        user.Should().NotBeNull();
        user!.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
    }
}
