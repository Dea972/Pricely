namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>
/// Billed searches of the day (UTC), in memory: one API instance (ADR-012). Checked before a search, counted after,
/// with the number of searches the provider actually billed.
/// </summary>
internal sealed class WebSearchQuota
{
    private readonly Lock _lock = new();
    private DateOnly _day;
    private int _used;

    public bool HasRoom(DateTime utcNow, int limit)
    {
        lock (_lock)
        {
            Roll(utcNow);

            return _used < limit;
        }
    }

    public void Count(DateTime utcNow, int searches)
    {
        lock (_lock)
        {
            Roll(utcNow);
            _used += searches;
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
