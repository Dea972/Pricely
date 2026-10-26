using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Pricely.Web.Api;
using Pricely.Web.Authentication;

namespace Pricely.Web.UnitTests;

/// <summary>Answers each request with the given function and keeps the requests.</summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>The body of each request, read when it is sent (the client disposes the request afterwards).</summary>
    public List<string?> Bodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

        return respond(request);
    }
}

internal static class Responses
{
    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Problem(HttpStatusCode status, string code, string detail) =>
        new(status)
        {
            Content = new StringContent($$"""{"title":"{{code}}","detail":"{{detail}}","status":{{(int)status}}}""", Encoding.UTF8, "application/problem+json")
        };

    public static HttpResponseMessage Events(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/event-stream") };
}

/// <summary>A fake HTTP server: its handler belongs to the <see cref="TestSession"/> that created it.</summary>
internal sealed class FakeServer(StubHandler handler)
{
    internal StubHandler Handler { get; } = handler;

    public List<HttpRequestMessage> Requests => Handler.Requests;

    public List<string?> Bodies => Handler.Bodies;
}

internal sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
}

internal sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

/// <summary>A signed-in session whose tokens live in a real <see cref="ServerTokenStore"/>.</summary>
internal sealed class TestSession : IDisposable
{
    public const string SessionKey = "session-1";
    public static readonly Uri TokenEndpoint = new("http://keycloak.test/realms/pricely/protocol/openid-connect/token");

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly List<IDisposable> _owned = [];

    public TestSession(Func<HttpRequestMessage, HttpResponseMessage>? keycloak = null)
    {
        Keycloak = Server(keycloak ?? (_ => new HttpResponseMessage(HttpStatusCode.BadRequest)));
        Store = new ServerTokenStore(_cache);
        var oidc = new OpenIdConnectOptions
        {
            ClientId = "pricely-web",
            ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                new OpenIdConnectConfiguration { TokenEndpoint = TokenEndpoint.AbsoluteUri })
        };

        Refresher = new TokenRefresher(
            new SingleClientFactory(Keycloak.Handler),
            new FixedOptionsMonitor<OpenIdConnectOptions>(oidc),
            Clock,
            NullLogger<TokenRefresher>.Instance);

        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PricelyClaims.Session, SessionKey), new Claim(PricelyClaims.Name, "Dominique Test")], "test"));
        Tokens = new AccessTokenProvider(new FixedAuthenticationStateProvider(user), Store, Refresher, Clock);
    }

    public FakeServer Keycloak { get; }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero));

    public ServerTokenStore Store { get; }

    public TokenRefresher Refresher { get; }

    public AccessTokenProvider Tokens { get; }

    public TestSession SignedIn(TimeSpan validFor, string accessToken = "access-1")
    {
        Store.Set(SessionKey, new TokenSet(accessToken, "refresh-1", "id-1", Clock.GetUtcNow().Add(validFor)));

        return this;
    }

    public FakeServer Server(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        _owned.Add(handler);

        return new FakeServer(handler);
    }

    public PricelyApiClient Api(FakeServer server) => new(Client(server), Tokens);

    public AgentStreamClient Agent(FakeServer server) => new(Client(server), Tokens);

    public void Dispose()
    {
        foreach (IDisposable owned in _owned)
        {
            owned.Dispose();
        }

        _cache.Dispose();
    }

    private HttpClient Client(FakeServer server)
    {
        var http = new HttpClient(server.Handler, disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
        _owned.Add(http);

        return http;
    }
}
