namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>
/// Billed searches of the day (UTC), in memory: one API instance (ADR-012). A request reserves the searches it may run
/// before it starts, so that concurrent requests cannot go over the limit, and gives back what was not billed.
/// </summary>
internal sealed class WebSearchQuota
{
    private readonly Lock _lock = new();
    private DateOnly _day;
    private int _used;

    /// <summary>
    /// Reserves up to <paramref name="searches"/> searches within what is left of the day: the number reserved, 0 when
    /// the day is used up.
    /// </summary>
    public int Reserve(DateTime utcNow, int limit, int searches)
    {
        lock (_lock)
        {
            Roll(utcNow);
            int reserved = Math.Min(searches, limit - _used);
            if (reserved <= 0)
            {
                return 0;
            }

            _used += reserved;

            return reserved;
        }
    }

    /// <summary>Replaces a reservation made on <paramref name="reservedOnUtc"/> by the searches actually billed.</summary>
    public void Settle(DateTime reservedOnUtc, int reserved, int billed)
    {
        lock (_lock)
        {
            if (DateOnly.FromDateTime(reservedOnUtc) == _day)
            {
                _used = Math.Max(0, _used - reserved + billed);
            }
        }
    }

    private void Roll(DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        if (today != _day)
        {
            _day = today;
            _used = 0;
        }
    }
}
