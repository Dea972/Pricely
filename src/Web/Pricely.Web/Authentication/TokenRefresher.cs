using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Pricely.Web.Authentication;

/// <summary>
/// Exchanges the refresh token for new tokens at the Keycloak token endpoint (public client: no secret).
/// </summary>
internal sealed class TokenRefresher(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    TimeProvider timeProvider,
    ILogger<TokenRefresher> logger)
{
    public const string HttpClientName = "keycloak";

    /// <summary>The new tokens, or null when Keycloak refuses (session ended, refresh token expired).</summary>
    public async Task<TokenSet?> RefreshAsync(TokenSet tokens, CancellationToken cancellationToken)
    {
        if (tokens.RefreshToken is null)
        {
            return null;
        }

        OpenIdConnectOptions options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        OpenIdConnectConfiguration configuration = await options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = options.ClientId!,
            ["refresh_token"] = tokens.RefreshToken
        });

        HttpClient http = httpClientFactory.CreateClient(HttpClientName);
        using HttpResponseMessage response = await http.PostAsync(new Uri(configuration.TokenEndpoint), content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("The refresh of the tokens was refused ({StatusCode}): the user has to sign in again", (int)response.StatusCode);

            return null;
        }

        TokenResponse? refreshed = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        return refreshed?.AccessToken is null
            ? null
            : new TokenSet(
                refreshed.AccessToken,
                refreshed.RefreshToken ?? tokens.RefreshToken,
                refreshed.IdToken ?? tokens.IdToken,
                timeProvider.GetUtcNow().AddSeconds(refreshed.ExpiresIn));
    }

    /// <summary>Tokens of the authorization code response, stored when the user signs in.</summary>
    public TokenSet FromSignIn(OpenIdConnectMessage response) =>
        new(
            response.AccessToken,
            response.RefreshToken,
            response.IdToken,
            timeProvider.GetUtcNow().AddSeconds(int.TryParse(response.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) ? seconds : 0));

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
