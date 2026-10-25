using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Exceptions;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Infrastructure.Authorization;

namespace Pricely.Common.UnitTests.Authorization;

public sealed class CustomClaimsTransformationTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Transform_ShouldAddTheUserIdAndThePermissions_AndKeepTheIdentityIdAsSub()
    {
        var permissionService = new FakePermissionService(Result.Success(new PermissionsResponse(UserId, ["users:read", "admin:access"])));
        CustomClaimsTransformation transformation = Create(permissionService);

        ClaimsPrincipal principal = await transformation.TransformAsync(Authenticated("kc-123", "dev@pricely.local", "Dev", "Pricely"));

        principal.GetUserId().Should().Be(UserId);
        principal.GetIdentityId().Should().Be("kc-123");
        principal.GetPermissions().Should().BeEquivalentTo("users:read", "admin:access");
        permissionService.Calls.Should().ContainSingle().Which.Should().Be(
            new IdentityProfile("kc-123", "dev@pricely.local", "Dev", "Pricely", "dev"));
    }

    [Fact]
    public async Task Transform_ShouldResolveTheUserOnce_WhenCalledTwiceOnTheSamePrincipal()
    {
        var permissionService = new FakePermissionService(Result.Success(new PermissionsResponse(UserId, ["users:read"])));
        CustomClaimsTransformation transformation = Create(permissionService);
        ClaimsPrincipal principal = Authenticated("kc-123", "dev@pricely.local", "Dev", "Pricely");

        await transformation.TransformAsync(principal);
        await transformation.TransformAsync(principal);

        permissionService.Calls.Should().HaveCount(1);
        principal.FindAll(CustomClaims.UserId).Should().ContainSingle();
    }

    [Fact]
    public async Task Transform_ShouldLeaveAnAnonymousPrincipalUntouched()
    {
        var permissionService = new FakePermissionService(Result.Success(new PermissionsResponse(UserId, [])));
        CustomClaimsTransformation transformation = Create(permissionService);

        ClaimsPrincipal principal = await transformation.TransformAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        permissionService.Calls.Should().BeEmpty();
        principal.HasClaim(c => c.Type == CustomClaims.UserId).Should().BeFalse();
    }

    [Fact]
    public async Task Transform_ShouldThrow_WhenTheUserCannotBeResolved()
    {
        var permissionService = new FakePermissionService(Result.Failure<PermissionsResponse>(Error.Problem("Users.MissingEmail", "No email")));
        CustomClaimsTransformation transformation = Create(permissionService);

        Func<Task> act = () => transformation.TransformAsync(Authenticated("kc-123", null, null, null));

        await act.Should().ThrowAsync<PricelyException>();
    }

    private static CustomClaimsTransformation Create(FakePermissionService permissionService)
    {
        ServiceProvider services = new ServiceCollection()
            .AddScoped<IPermissionService>(_ => permissionService)
            .BuildServiceProvider();

        return new CustomClaimsTransformation(services.GetRequiredService<IServiceScopeFactory>());
    }

    private static ClaimsPrincipal Authenticated(string sub, string? email, string? givenName, string? familyName)
    {
        var claims = new List<Claim> { new(CustomClaims.Sub, sub), new(CustomClaims.PreferredUserName, "dev") };

        if (email is not null)
        {
            claims.Add(new Claim(CustomClaims.Email, email));
        }

        if (givenName is not null)
        {
            claims.Add(new Claim(CustomClaims.GivenName, givenName));
        }

        if (familyName is not null)
        {
            claims.Add(new Claim(CustomClaims.FamilyName, familyName));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private sealed class FakePermissionService(Result<PermissionsResponse> result) : IPermissionService
    {
        public List<IdentityProfile> Calls { get; } = [];

        public Task<Result<PermissionsResponse>> GetUserPermissionsAsync(IdentityProfile identity, CancellationToken cancellationToken = default)
        {
            Calls.Add(identity);

            return Task.FromResult(result);
        }
    }
}
