using System.Globalization;
using Pricely.Web.Api;

namespace Pricely.Web.Components.Shared;

/// <summary>How the front writes amounts, dates and the values of the API, in French.</summary>
public static class Format
{
    public static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public static string Price(decimal amount, string? currency) =>
        string.Equals(currency?.Trim(), "EUR", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(currency)
            ? amount.ToString("N2", French) + " €"
            : amount.ToString("N2", French) + " " + currency.Trim();

    public static string Price(decimal? amount, string? currency) => amount is decimal value ? Price(value, currency) : "—";

    /// <summary>A relative deviation (0.046 → « +4,6 % »), with a true minus sign.</summary>
    public static string Deviation(decimal fraction)
    {
        decimal percent = Math.Round(fraction * 100, 1);
        string sign = percent switch
        {
            > 0 => "+",
            < 0 => "−",
            _ => string.Empty
        };

        return sign + Math.Abs(percent).ToString("N1", French) + " %";
    }

    /// <summary>« il y a 2 h », « hier », then the date.</summary>
    public static string Ago(DateTime utc, DateTimeOffset now)
    {
        TimeSpan elapsed = now.UtcDateTime - DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "à l'instant",
            { TotalHours: < 1 } => $"il y a {(int)elapsed.TotalMinutes} min",
            { TotalHours: < 24 } => $"il y a {(int)elapsed.TotalHours} h",
            { TotalHours: < 48 } => "hier",
            _ => Date(utc)
        };
    }

    public static string Date(DateTime utc) => utc.ToString("d MMM yyyy", French);

    public static string Availability(string availability) =>
        availability switch
        {
            ApiValues.InStock => "En stock",
            ApiValues.OutOfStock => "Rupture",
            _ => "Inconnue"
        };

    public static string MatchMethod(string? method) =>
        method switch
        {
            ApiValues.Gtin => "GTIN identique",
            ApiValues.ManufacturerReference => "Marque + réf. fabricant",
            ApiValues.Manual => "Validée à la main",
            _ => "À valider"
        };

    public static string OfferStatus(string status) =>
        status switch
        {
            ApiValues.Confirmed => "Confirmée",
            ApiValues.Proposed => "Proposée",
            ApiValues.Rejected => "Rejetée",
            _ => status
        };

    /// <summary>Cost in USD (AiEngine).</summary>
    public static string Cost(decimal cost) => cost.ToString("N4", French) + " $";

    public static string Count(long value) => value.ToString("N0", French);

    /// <summary>
    /// A link to a merchant page, which comes from collected data: only an absolute http(s) address, never a
    /// <c>javascript:</c> or <c>data:</c> one.
    /// </summary>
    public static string? ExternalHref(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.AbsoluteUri
            : null;

    /// <summary>Two letters of a name for the avatar.</summary>
    public static string Initials(string? name)
    {
        string[] words = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.Length switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
            _ => string.Concat(words[0][0], words[^1][0]).ToUpperInvariant()
        };
    }
}
