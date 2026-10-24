using Pricely.Common.Application.Authorization;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.GetUserPermissions;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.IntegrationTests.Abstractions;
using FluentAssertions;

namespace Pricely.Modules.Users.IntegrationTests.Users;

public class GetUserPermissionsValidationTests : BaseIntegrationTest
{
    public GetUserPermissionsValidationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }



    [Fact]
    public async Task Should_ReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        string nonExistentIdentityId = Guid.NewGuid().ToString();

        // Act
        Result<PermissionsResponse> result = await Sender.Send(
            new GetUserPermissionsQuery(nonExistentIdentityId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound(nonExistentIdentityId));
    }

    [Fact]
    public async Task Should_ReturnPermissionsResponse_WithUserId()
    {
        // Arrange
        Result<Guid> registerResult = await Sender.Send(new RegisterUserCommand(
            Faker.Internet.Email(),
            Faker.Internet.UserName(),
            Faker.Name.FirstName(),
            Faker.Name.LastName()));

        User? user = await DbContext.Users.FindAsync(registerResult.Value);
        string identityId = user!.IdentityId;

        // Act
        Result<PermissionsResponse> result = await Sender.Send(
            new GetUserPermissionsQuery(identityId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(registerResult.Value);
        result.Value.Permissions.Should().NotBeNull();
    }
}
