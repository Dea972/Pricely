using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Pricely.Web.Authentication;

/// <summary>
/// Tokens of the signed-in users, in the memory of the server, by session key (a claim of the authentication cookie).
/// The browser never receives a token, not even encrypted in the cookie. A restart loses them: the user signs in again
/// (one instance only; several would need a shared cache, ADR-015).
/// </summary>
internal sealed class ServerTokenStore(IMemoryCache cache)
{
    /// <summary>Same lifetime as the authentication cookie.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public TokenSet? Get(string sessionKey) => cache.Get<TokenSet>(Key(sessionKey));

    public void Set(string sessionKey, TokenSet tokens)
    {
        var options = new MemoryCacheEntryOptions { SlidingExpiration = SessionLifetime };

        // A session that just expires also gives back its lock.
        options.RegisterPostEvictionCallback((_, _, reason, _) =>
        {
            if (reason is EvictionReason.Expired or EvictionReason.Removed or EvictionReason.Capacity)
            {
                _locks.TryRemove(sessionKey, out _);
            }
        });

        cache.Set(Key(sessionKey), tokens, options);
    }

    public void Remove(string sessionKey)
    {
        cache.Remove(Key(sessionKey));

        // Not disposed: a refresh may still be waiting on it (a SemaphoreSlim without wait handle holds no resource).
        _locks.TryRemove(sessionKey, out _);
    }

    /// <summary>One refresh at a time per session: two circuits of the same user must not race on the refresh token.</summary>
    public SemaphoreSlim LockFor(string sessionKey) => _locks.GetOrAdd(sessionKey, _ => new SemaphoreSlim(1, 1));

    private static string Key(string sessionKey) => "tokens:" + sessionKey;
}
