using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Infrastructure.Authentication;
using Pricely.Common.Infrastructure.Authorization;

namespace Pricely.Common.UnitTests.Authorization;

public sealed class ScopePolicyTests
{
    [Theory]
    [InlineData("openid profile pricely.read", PricelyScopes.Read, true)]
    [InlineData("pricely.write", PricelyScopes.Write, true)]
    [InlineData("openid pricely.review email", PricelyScopes.Review, true)]
    [InlineData("openid profile", PricelyScopes.Read, false)]
    [InlineData("pricely.write pricely.review", PricelyScopes.Read, false)]
    [InlineData("pricely.readonly", PricelyScopes.Read, false)]
    public async Task Policy_ShouldBeSatisfied_OnlyWhenTheTokenCarriesTheScope(string scopeClaim, string policy, bool expected)
    {
        await using ServiceProvider services = BuildServices();
        IAuthorizationService authorization = services.GetRequiredService<IAuthorizationService>();

        AuthorizationResult result = await authorization.AuthorizeAsync(User(scopeClaim), resource: null, policy);

        result.Succeeded.Should().Be(expected);
    }

    [Fact]
    public async Task Policy_ShouldFail_WhenTheCallerIsNotAuthenticated()
    {
        await using ServiceProvider services = BuildServices();
        IAuthorizationService authorization = services.GetRequiredService<IAuthorizationService>();

        AuthorizationResult result = await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), resource: null, PricelyScopes.Read);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task EveryScope_ShouldHaveAPolicyNamedAfterIt()
    {
        await using ServiceProvider services = BuildServices();
        IAuthorizationPolicyProvider provider = services.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (string scope in PricelyScopes.All)
        {
            AuthorizationPolicy? policy = await provider.GetPolicyAsync(scope);

            policy.Should().NotBeNull();
            policy!.Requirements.Should().ContainSingle(r => r.GetType().Name == nameof(ScopeRequirement));
        }
    }

    [Fact]
    public void GetScopes_ShouldSplitTheSpaceSeparatedClaim()
    {
        ClaimsPrincipal user = User("openid  pricely.read pricely.write");

        user.GetScopes().Should().BeEquivalentTo("openid", "pricely.read", "pricely.write");
    }

    private static ClaimsPrincipal User(string scopeClaim) =>
        new(new ClaimsIdentity([new Claim(CustomClaims.Scope, scopeClaim)], authenticationType: "test"));

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddAuthorizationInternal();

        return services.BuildServiceProvider();
    }
}
