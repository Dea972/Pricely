using Pricely.Common.Application.Clock;
using Pricely.Modules.Pricing.Application.WebResearch;

namespace Pricely.Modules.Pricing.Infrastructure.WebResearch;

/// <summary>
/// <see cref="ISearchedPages"/> in memory, for one API instance: the pages found in the last
/// <see cref="Retention"/>, bounded per research and in number of researches (the oldest research is forgotten first).
/// Several instances would need a shared store.
/// </summary>
internal sealed class InMemorySearchedPages(IDateTimeProvider dateTimeProvider) : ISearchedPages
{
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public const int MaxPagesPerResearch = 500;

    public const int MaxResearches = 5_000;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, Research> _researches = new(StringComparer.Ordinal);

    public void Remember(string research, IEnumerable<Uri> addresses)
    {
        DateTime now = dateTimeProvider.UtcNow;
        lock (_lock)
        {
            if (!_researches.TryGetValue(research, out Research? pages))
            {
                Forget(now);
                pages = new Research();
                _researches[research] = pages;
            }

            pages.LastUsedUtc = now;
            foreach (Uri address in addresses)
            {
                pages.FoundOnUtc[Key(address)] = now;
            }

            if (pages.FoundOnUtc.Count > MaxPagesPerResearch)
            {
                foreach (string oldest in pages.FoundOnUtc.OrderBy(p => p.Value).Take(pages.FoundOnUtc.Count - MaxPagesPerResearch).Select(p => p.Key).ToList())
                {
                    pages.FoundOnUtc.Remove(oldest);
                }
            }
        }
    }

    public bool WasFound(string research, Uri address)
    {
        DateTime now = dateTimeProvider.UtcNow;
        lock (_lock)
        {
            return _researches.TryGetValue(research, out Research? pages)
                && pages.FoundOnUtc.TryGetValue(Key(address), out DateTime foundOnUtc)
                && now - foundOnUtc <= Retention;
        }
    }

    /// <summary>The page without its fragment: the same page as far as the site is concerned.</summary>
    private static string Key(Uri address) => address.IsAbsoluteUri ? address.GetLeftPart(UriPartial.Query) : address.OriginalString;

    /// <summary>Drops the expired researches, then the least recently used ones beyond the bound.</summary>
    private void Forget(DateTime now)
    {
        foreach (string expired in _researches.Where(r => now - r.Value.LastUsedUtc > Retention).Select(r => r.Key).ToList())
        {
            _researches.Remove(expired);
        }

        if (_researches.Count >= MaxResearches)
        {
            foreach (string oldest in _researches.OrderBy(r => r.Value.LastUsedUtc).Take(_researches.Count - MaxResearches + 1).Select(r => r.Key).ToList())
            {
                _researches.Remove(oldest);
            }
        }
    }

    private sealed class Research
    {
        public Dictionary<string, DateTime> FoundOnUtc { get; } = new(StringComparer.Ordinal);

        public DateTime LastUsedUtc { get; set; }
    }
}
