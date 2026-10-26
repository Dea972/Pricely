using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Pricely.Web.Authentication;

/// <summary>
/// Sign-in with Keycloak on the server (ADR-015): authorization code + PKCE with the public client <c>pricely-web</c>,
/// tokens kept on the server (<see cref="ServerTokenStore"/>), only a session cookie in the browser.
/// </summary>
internal static class AuthenticationExtensions
{
    public static IServiceCollection AddPricelyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        OidcSettings settings = configuration.GetSection(OidcSettings.SectionName).Get<OidcSettings>()
            ?? throw new InvalidOperationException($"The section '{OidcSettings.SectionName}' is not configured.");

        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ServerTokenStore>();
        services.AddSingleton<TokenRefresher>();
        services.AddScoped<AccessTokenProvider>();
        services.AddHttpClient(TokenRefresher.HttpClientName);

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "pricely";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = ServerTokenStore.SessionLifetime;
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = settings.Authority;
                options.ClientId = settings.ClientId;
                options.ResponseType = "code";
                options.UsePkce = true;

                // Keycloak runs over plain HTTP on the developer machine only.
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.TokenValidationParameters.NameClaimType = PricelyClaims.Name;

                options.Scope.Clear();
                foreach (string scope in settings.Scopes)
                {
                    options.Scope.Add(scope);
                }

                options.Events.OnTokenValidated = StoreTokensAsync;
                options.Events.OnRedirectToIdentityProviderForSignOut = AddIdTokenHintAsync;
            });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("authentication");

        group.MapGet("login", (string? returnUrl) =>
                TypedResults.Challenge(new AuthenticationProperties { RedirectUri = LocalOrRoot(returnUrl) }))
            .AllowAnonymous();

        // POST with the antiforgery token of the form: a link cannot sign the user out.
        group.MapPost("logout", (HttpContext context, ServerTokenStore store) =>
            {
                if (context.User.FindFirst(PricelyClaims.Session)?.Value is string sessionKey)
                {
                    // The id token is still needed by Keycloak's end-session endpoint: read it before removing.
                    context.Items[nameof(TokenSet.IdToken)] = store.Get(sessionKey)?.IdToken;
                    store.Remove(sessionKey);
                }

                return TypedResults.SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
            })
            .WithMetadata(new RequireAntiforgeryTokenAttribute());

        return app;
    }

    private static Task StoreTokensAsync(TokenValidatedContext context)
    {
        if (context.TokenEndpointResponse is null || context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Keycloak returned no tokens.");

            return Task.CompletedTask;
        }

        IServiceProvider services = context.HttpContext.RequestServices;
        TokenSet tokens = services.GetRequiredService<TokenRefresher>().FromSignIn(context.TokenEndpointResponse);
        string sessionKey = Guid.NewGuid().ToString("N");

        services.GetRequiredService<ServerTokenStore>().Set(sessionKey, tokens);
        identity.AddClaim(new Claim(PricelyClaims.Session, sessionKey));

        return Task.CompletedTask;
    }

    private static Task AddIdTokenHintAsync(RedirectContext context)
    {
        if (context.HttpContext.Items[nameof(TokenSet.IdToken)] is string idToken)
        {
            context.ProtocolMessage.IdTokenHint = idToken;
        }

        return Task.CompletedTask;
    }

    /// <summary>Only a path of this site: an absolute or protocol-relative URL would be an open redirect.</summary>
    internal static string LocalOrRoot(string? returnUrl) =>
        returnUrl is { Length: > 0 }
            && returnUrl[0] == '/'
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";
}
