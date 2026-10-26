using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.GetUserPermissions;
using Pricely.Modules.Users.Application.Users.ProvisionUser;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Authorization;

namespace Pricely.Modules.Users.UnitTests.Provisioning;

public class PermissionServiceTests
{
    private static readonly IdentityProfile Identity = new("kc-sub-1", "dev@pricely.local", "Dev", "Pricely", "dev");
    private static readonly PermissionsResponse Known = new(Guid.NewGuid(), ["users:read"]);

    [Fact]
    public async Task Resolve_ShouldNotProvision_WhenTheUserExists()
    {
        var sender = new FakeSender { Permissions = [Result.Success(Known)] };

        Result<PermissionsResponse> result = await new PermissionService(sender).ResolveCallerAsync(Identity);

        result.Value.Should().Be(Known);
        sender.Provisioned.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_ShouldProvisionTheUserFromTheTokenClaims_ThenReadItBack()
    {
        var sender = new FakeSender { Permissions = [NotFound, Result.Success(Known)] };

        Result<PermissionsResponse> result = await new PermissionService(sender).ResolveCallerAsync(Identity);

        result.Value.Should().Be(Known);
        sender.Provisioned.Should().ContainSingle().Which.Should().Be(
            new ProvisionUserCommand("kc-sub-1", "dev@pricely.local", "Dev", "Pricely"));
    }

    [Fact]
    public async Task Resolve_ShouldFail_WhenTheTokenHasNoEmail()
    {
        var sender = new FakeSender { Permissions = [NotFound] };

        Result<PermissionsResponse> result = await new PermissionService(sender)
            .ResolveCallerAsync(Identity with { Email = null });

        result.Error.Should().Be(UserErrors.MissingEmail);
        sender.Provisioned.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_ShouldReadTheUserBack_WhenAConcurrentRequestCreatedIt()
    {
        var sender = new FakeSender
        {
            Permissions = [NotFound, Result.Success(Known)],
            ProvisionFailure = new PricelyException("ProvisionUserCommand", innerException: new DbUpdateException("duplicate identity_id"))
        };

        Result<PermissionsResponse> result = await new PermissionService(sender).ResolveCallerAsync(Identity);

        result.Value.Should().Be(Known);
    }

    [Fact]
    public async Task Resolve_ShouldFail_WhenTheEmailBelongsToAnotherIdentity()
    {
        var sender = new FakeSender
        {
            Permissions = [NotFound, NotFound],
            ProvisionFailure = new PricelyException("ProvisionUserCommand", innerException: new DbUpdateException("duplicate email"))
        };

        Result<PermissionsResponse> result = await new PermissionService(sender).ResolveCallerAsync(Identity);

        result.Error.Should().Be(UserErrors.EmailAlreadyLinked);
    }

    private static Result<PermissionsResponse> NotFound => Result.Failure<PermissionsResponse>(UserErrors.NotFound("kc-sub-1"));

    private sealed class FakeSender : ISender
    {
        private int _permissionCalls;

        public required List<Result<PermissionsResponse>> Permissions { get; init; }

        public PricelyException? ProvisionFailure { get; init; }

        public List<ProvisionUserCommand> Provisioned { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            switch (request)
            {
                case GetUserPermissionsQuery:
                    return Task.FromResult((TResponse)(object)Permissions[Math.Min(_permissionCalls++, Permissions.Count - 1)]);
                case ProvisionUserCommand command:
                    if (ProvisionFailure is not null)
                    {
                        throw ProvisionFailure;
                    }

                    Provisioned.Add(command);

                    return Task.FromResult((TResponse)(object)Result.Success(Guid.NewGuid()));
                default:
                    throw new NotSupportedException(request.GetType().Name);
            }
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
