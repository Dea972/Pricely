using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Pricely.Web.Authentication;

/// <summary>
/// Tokens of the signed-in users, on the server only, by session key (a claim of the authentication cookie). The browser
/// never receives a token, not even encrypted in the cookie. They live in the distributed cache, encrypted with the data
/// protection keys: in memory by default (a restart signs the users out), shared by every instance once a shared cache
/// and shared keys are configured (ADR-015).
/// </summary>
internal sealed class ServerTokenStore(IDistributedCache cache, IDataProtectionProvider dataProtection)
{
    /// <summary>Same lifetime as the authentication cookie.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    /// <summary>
    /// One refresh at a time per session on this instance (two circuits of the same user must not race on the refresh
    /// token): a fixed set of locks, picked by session, so that expired sessions leave nothing behind.
    /// </summary>
    private static readonly SemaphoreSlim[] RefreshLocks = [.. Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1))];

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Pricely.Web.Tokens");

    public async Task<TokenSet?> GetAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        byte[]? stored = await cache.GetAsync(Key(sessionKey), cancellationToken);
        if (stored is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TokenSet>(_protector.Unprotect(stored));
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Encrypted with keys this instance does not have (keys not shared, or rotated away): the user signs in again.
            return null;
        }
    }

    public Task SetAsync(string sessionKey, TokenSet tokens, CancellationToken cancellationToken = default) =>
        cache.SetAsync(
            Key(sessionKey),
            _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(tokens)),
            new DistributedCacheEntryOptions { SlidingExpiration = SessionLifetime },
            cancellationToken);

    public Task RemoveAsync(string sessionKey, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(Key(sessionKey), cancellationToken);

    public static SemaphoreSlim LockFor(string sessionKey) =>
        RefreshLocks[(int)((uint)StringComparer.Ordinal.GetHashCode(sessionKey) % RefreshLocks.Length)];

    private static string Key(string sessionKey) => "pricely-web:tokens:" + sessionKey;
}
