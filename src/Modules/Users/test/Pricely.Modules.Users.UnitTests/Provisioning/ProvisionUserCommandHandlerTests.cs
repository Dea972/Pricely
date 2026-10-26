using FluentAssertions;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Abstractions.Data;
using Pricely.Modules.Users.Application.Users.ProvisionUser;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.UnitTests.Provisioning;

public class ProvisionUserCommandHandlerTests
{
    private static readonly ProvisionUserCommand Command = new("kc-sub-1", "dev@pricely.local", "Dev", "Pricely");

    [Fact]
    public async Task Handle_ShouldCreateAMember_WhenTheIdentityIsUnknown()
    {
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ProvisionUserCommandHandler(repository, unitOfWork);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        User user = repository.Inserted.Should().ContainSingle().Subject;
        user.Id.Should().Be(result.Value);
        user.IdentityId.Should().Be("kc-sub-1");
        user.Email.Should().Be("dev@pricely.local");
        user.Roles.Single().Should().Be(Role.Member);
        unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheExistingUser_WhenTheIdentityIsKnown()
    {
        var existing = User.Create("dev@pricely.local", "Dev", "Pricely", "kc-sub-1");
        var repository = new FakeUserRepository { Existing = existing };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ProvisionUserCommandHandler(repository, unitOfWork);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.Value.Should().Be(existing.Id);
        repository.Inserted.Should().BeEmpty();
        unitOfWork.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData("", "dev@pricely.local")]
    [InlineData("kc-sub-1", "")]
    [InlineData("kc-sub-1", "not-an-email")]
    public void Validator_ShouldReject_AMissingIdentityOrAnInvalidEmail(string identityId, string email)
    {
        var validator = new ProvisionUserCommandValidator();

        validator.Validate(new ProvisionUserCommand(identityId, email, "Dev", "Pricely")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShouldAccept_ACompleteCommand()
    {
        new ProvisionUserCommandValidator().Validate(Command).IsValid.Should().BeTrue();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? Existing { get; init; }

        public List<User> Inserted { get; } = [];

        public Task<User?> GetAsync(string identityId, CancellationToken cancellationToken = default) => Task.FromResult(Existing);

        public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Existing);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Existing);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(Existing);

        public void Insert(User user) => Inserted.Add(user);

        public void UntrackStaticEntities()
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }
}
