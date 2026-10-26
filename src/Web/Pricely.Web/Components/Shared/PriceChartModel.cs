using System.Globalization;
using Pricely.Web.Api;

namespace Pricely.Web.Components.Shared;

/// <summary>
/// Geometry of the price history chart (SVG 640 × 260): one line per merchant. Observations flagged by the price guard
/// are left out of the lines (one pallet price would flatten every other line) and counted apart.
/// </summary>
public sealed class PriceChartModel
{
    public const int Width = 640;
    public const int Height = 260;
    private const double Left = 56;
    private const double Right = 630;
    private const double Top = 20;
    private const double Bottom = 200;

    private PriceChartModel(IReadOnlyList<ChartSeries> series, IReadOnlyList<ChartTick> yTicks, IReadOnlyList<ChartTick> xTicks, int flaggedCount)
    {
        Series = series;
        YTicks = yTicks;
        XTicks = xTicks;
        FlaggedCount = flaggedCount;
    }

    public IReadOnlyList<ChartSeries> Series { get; }

    public IReadOnlyList<ChartTick> YTicks { get; }

    public IReadOnlyList<ChartTick> XTicks { get; }

    public int FlaggedCount { get; }

    public static IReadOnlyList<double> GridLines { get; } = [Top, Top + (Bottom - Top) / 3, Top + 2 * (Bottom - Top) / 3, Bottom];

    /// <summary>For screen readers: the first and last price of each line.</summary>
    public string Description =>
        Series.Count == 0
            ? "Aucun relevé à tracer."
            : "Évolution des prix pour " + Series.Count.ToString(CultureInfo.InvariantCulture) + " enseigne(s) : "
                + string.Join(" ; ", Series.Select(s => $"{s.Name} de {Format.Price(s.FirstAmount, s.Currency)} à {Format.Price(s.LastAmount, s.Currency)}"));

    public static PriceChartModel Build(IEnumerable<PriceHistoryEntryDto> entries)
    {
        var all = entries.ToList();
        var points = all.Where(e => !e.IsFlaggedAsAnomaly).OrderBy(e => e.ObservedOnUtc).ToList();
        int flagged = all.Count - points.Count;

        if (points.Count == 0)
        {
            return new PriceChartModel([], [], [], flagged);
        }

        DateTime first = points[0].ObservedOnUtc;
        DateTime last = points[^1].ObservedOnUtc;
        double span = Math.Max((last - first).TotalSeconds, 1);

        decimal min = points.Min(p => p.Amount);
        decimal max = points.Max(p => p.Amount);
        decimal padding = Math.Max((max - min) * 0.1m, Math.Max(max * 0.02m, 0.01m));
        decimal low = Math.Max(0, min - padding);
        decimal high = max + padding;

        double X(DateTime at) => points.Count == 1 || first == last ? (Left + Right) / 2 : Left + (at - first).TotalSeconds / span * (Right - Left);
        double Y(decimal amount) => Bottom - (double)((amount - low) / (high - low)) * (Bottom - Top);

        var series = points
            .GroupBy(p => (p.MerchantId, p.MerchantName))
            .Select((group, index) =>
            {
                var ordered = group.OrderBy(p => p.ObservedOnUtc).ToList();
                string polyline = string.Join(' ', ordered.Select(p => Point(X(p.ObservedOnUtc), Y(p.Amount))));

                return new ChartSeries(
                    group.Key.MerchantName,
                    polyline,
                    X(ordered[^1].ObservedOnUtc),
                    Y(ordered[^1].Amount),
                    ordered[0].Amount,
                    ordered[^1].Amount,
                    ordered[0].Currency,
                    index % SeriesStyles.Count);
            })
            .ToList();

        var yTicks = GridLines
            .Select(y => new ChartTick(y, (high - (decimal)((y - Top) / (Bottom - Top)) * (high - low)).ToString("N2", Format.French)))
            .ToList();

        var xTicks = Enumerable.Range(0, 4)
            .Select(i => first.AddSeconds(span * i / 3))
            .Select(at => new ChartTick(X(at), at.ToString("d MMM", Format.French)))
            .DistinctBy(t => t.Label, StringComparer.Ordinal)
            .ToList();

        return new PriceChartModel(series, yTicks, xTicks, flagged);
    }

    /// <summary>Colour and dash of each line: a style of line per series, the colour is not the only signal.</summary>
    public static IReadOnlyList<(string Color, string? Dash)> SeriesStyles { get; } =
        [("#0B5D6B", null), ("#8A6FB8", "6 4"), ("#B4762F", "2 4")];

    private static string Point(double x, double y) =>
        x.ToString("0.#", CultureInfo.InvariantCulture) + "," + y.ToString("0.#", CultureInfo.InvariantCulture);
}

public sealed record ChartSeries(
    string Name,
    string Points,
    double LastX,
    double LastY,
    decimal FirstAmount,
    decimal LastAmount,
    string Currency,
    int Style);

public sealed record ChartTick(double Position, string Label);
