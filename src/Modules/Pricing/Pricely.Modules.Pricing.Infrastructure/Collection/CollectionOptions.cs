namespace Pricely.Modules.Pricing.Infrastructure.Collection;

internal sealed class CollectionOptions
{
    /// <summary>
    /// Disabled by default: enable it once a real collector is configured.
    /// </summary>
    public bool Enabled { get; init; }

    public int IntervalInMinutes { get; init; } = 24 * 60;
}
