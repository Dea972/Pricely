using System.Net;
using FluentAssertions;
using Pricely.Web.Authentication;

namespace Pricely.Web.UnitTests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task AccessToken_ShouldBeTheStoredOne_WhileItIsFresh()
    {
        using TestSession session = new TestSession().SignedIn(TimeSpan.FromMinutes(10));

        string token = await session.Tokens.GetAccessTokenAsync(CancellationToken.None);

        token.Should().Be("access-1");
    }

    [Fact]
    public async Task AccessToken_ShouldBeRefreshed_ShortlyBeforeItExpires()
    {
        using TestSession session = new TestSession(_ => Responses.Json("""{"access_token":"access-2","refresh_token":"refresh-2","expires_in":900}""")).SignedIn(TimeSpan.FromSeconds(30));
        FakeServer keycloak = session.Keycloak;

        string token = await session.Tokens.GetAccessTokenAsync(CancellationToken.None);

        token.Should().Be("access-2");
        HttpRequestMessage refresh = keycloak.Requests.Should().ContainSingle().Subject;
        refresh.RequestUri.Should().Be(TestSession.TokenEndpoint);
        keycloak.Bodies[0].Should().Contain("grant_type=refresh_token").And.Contain("refresh_token=refresh-1").And.Contain("client_id=pricely-web");

        TokenSet stored = session.Store.Get(TestSession.SessionKey)!;
        stored.RefreshToken.Should().Be("refresh-2");
        stored.IdToken.Should().Be("id-1", "Keycloak does not always send a new id token: the one of the sign-in is kept for the sign-out");
        stored.ExpiresAt.Should().Be(session.Clock.GetUtcNow().AddSeconds(900));
    }

    [Fact]
    public async Task ARefusedRefresh_ShouldEndTheSession()
    {
        using TestSession session = new TestSession(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)).SignedIn(TimeSpan.Zero);

        Func<Task> act = () => session.Tokens.GetAccessTokenAsync(CancellationToken.None);

        await act.Should().ThrowAsync<SessionExpiredException>();
        session.Store.Get(TestSession.SessionKey).Should().BeNull();
    }

    [Fact]
    public async Task KeycloakOutOfOrder_ShouldNotEndTheSession()
    {
        using TestSession session = new TestSession(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)).SignedIn(TimeSpan.Zero);

        Func<Task> act = () => session.Tokens.GetAccessTokenAsync(CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        session.Store.Get(TestSession.SessionKey).Should().NotBeNull("the next call tries the refresh again");
    }

    [Fact]
    public async Task ASessionWithoutTokens_ShouldBeExpired()
    {
        // The server restarted: the cookie is still valid, the tokens are gone.
        using var session = new TestSession();

        Func<Task> act = () => session.Tokens.GetAccessTokenAsync(CancellationToken.None);

        await act.Should().ThrowAsync<SessionExpiredException>();
    }

    [Theory]
    [InlineData("/products/1", "/products/1")]
    [InlineData("/", "/")]
    [InlineData("https://evil.test/", "/")]
    [InlineData("//evil.test/", "/")]
    [InlineData("/\\evil.test", "/")]
    [InlineData("/\t/evil.test", "/")]
    [InlineData("/\r\n/evil.test", "/")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    public void SignIn_ShouldOnlyReturnToAPageOfTheSite(string? returnAddress, string expected) =>
        AuthenticationExtensions.LocalOrRoot(returnAddress).Should().Be(expected);
}
