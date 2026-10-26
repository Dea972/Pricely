using Microsoft.AspNetCore.Components.Authorization;

namespace Pricely.Web.Authentication;

/// <summary>
/// The access token of the current user for the calls to the API, refreshed shortly before it expires. Works inside an
/// interactive circuit, where there is no <c>HttpContext</c>: the session key comes from the authentication state.
/// </summary>
internal sealed class AccessTokenProvider(
    AuthenticationStateProvider authenticationStateProvider,
    ServerTokenStore store,
    TokenRefresher refresher,
    TimeProvider timeProvider)
{
    /// <summary>A token that expires sooner is refreshed first: a turn of the agent can last a while.</summary>
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        AuthenticationState state = await authenticationStateProvider.GetAuthenticationStateAsync();
        string sessionKey = state.User.FindFirst(PricelyClaims.Session)?.Value ?? throw new SessionExpiredException();

        TokenSet tokens = store.Get(sessionKey) ?? throw new SessionExpiredException();
        if (IsFresh(tokens))
        {
            return tokens.AccessToken;
        }

        SemaphoreSlim gate = store.LockFor(sessionKey);
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Another circuit of the same session may have refreshed them in the meantime.
            tokens = store.Get(sessionKey) ?? throw new SessionExpiredException();
            if (IsFresh(tokens))
            {
                return tokens.AccessToken;
            }

            TokenSet? refreshed = await refresher.RefreshAsync(tokens);
            if (refreshed is null)
            {
                store.Remove(sessionKey);

                throw new SessionExpiredException();
            }

            store.Set(sessionKey, refreshed);

            return refreshed.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool IsFresh(TokenSet tokens) => tokens.ExpiresAt - RefreshMargin > timeProvider.GetUtcNow();
}
