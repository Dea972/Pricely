using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Http.Resilience;

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

        // In memory by default; several instances need a shared cache (AddStackExchangeRedisCache...) and shared data
        // protection keys, so that each one reads the tokens of the others (ADR-015).
        services.AddDistributedMemoryCache();
        services.AddDataProtection().SetApplicationName("Pricely.Web");
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ServerTokenStore>();
        services.AddSingleton<TokenRefresher>();
        services.AddScoped<AccessTokenProvider>();
        services.AddHttpClient(TokenRefresher.HttpClientName);

        // A refresh token may be rotated by Keycloak: the exchange is never sent twice.
        services.Configure<HttpStandardResilienceOptions>(
            $"{TokenRefresher.HttpClientName}-standard",
            options => options.Retry.DisableForUnsafeHttpMethods());

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

                // Behind a TLS reverse proxy, forwarded headers must be set up as well (ADR-015).
                options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
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

        // POST with the antiforgery token of the form: neither a link nor another site can sign the user out. The token is
        // checked here: the antiforgery middleware only rejects endpoints that read a form.
        group.MapPost("logout", async Task<Results<BadRequest, SignOutHttpResult>> (HttpContext context, IAntiforgery antiforgery, ServerTokenStore store) =>
            {
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    return TypedResults.BadRequest();
                }

                if (context.User.FindFirst(PricelyClaims.Session)?.Value is string sessionKey)
                {
                    // The id token is still needed by Keycloak's end-session endpoint: read it before removing.
                    context.Items[nameof(TokenSet.IdToken)] = (await store.GetAsync(sessionKey, context.RequestAborted))?.IdToken;
                    await store.RemoveAsync(sessionKey, context.RequestAborted);
                }

                return TypedResults.SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
            })
            .DisableAntiforgery();

        return app;
    }

    private static async Task StoreTokensAsync(TokenValidatedContext context)
    {
        if (context.TokenEndpointResponse is null || context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Keycloak returned no tokens.");

            return;
        }

        IServiceProvider services = context.HttpContext.RequestServices;
        TokenSet tokens = services.GetRequiredService<TokenRefresher>().FromSignIn(context.TokenEndpointResponse);
        string sessionKey = Guid.NewGuid().ToString("N");

        await services.GetRequiredService<ServerTokenStore>().SetAsync(sessionKey, tokens, context.HttpContext.RequestAborted);
        identity.AddClaim(new Claim(PricelyClaims.Session, sessionKey));
    }

    private static Task AddIdTokenHintAsync(RedirectContext context)
    {
        if (context.HttpContext.Items[nameof(TokenSet.IdToken)] is string idToken)
        {
            context.ProtocolMessage.IdTokenHint = idToken;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Only a path of this site, by the rule of ASP.NET Core's <c>IsLocalUrl</c>: an absolute or protocol-relative
    /// address would be an open redirect, and browsers drop control characters (« /\t/evil.test » is « //evil.test »).
    /// </summary>
    internal static string LocalOrRoot(string? returnUrl) =>
        returnUrl is { Length: > 0 }
            && returnUrl[0] == '/'
            && (returnUrl.Length == 1 || returnUrl[1] != '/' && returnUrl[1] != '\\')
            && !returnUrl.Any(char.IsControl)
            ? returnUrl
            : "/";
}
