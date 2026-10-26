using System.Globalization;
using System.Text.RegularExpressions;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Domain.PageSnapshots;

/// <summary>
/// The text of a merchant page as Pricely fetched it (ADR-009): a price submitted by the collection agent is accepted
/// only if it appears in a snapshot of that merchant's site. Kept as the trace of the collection.
/// </summary>
public sealed partial class PageSnapshot : Entity
{
    /// <summary>Longest text kept: enough for a product page once the markup is removed.</summary>
    public const int ContentMaxLength = 200_000;

    public const int AddressMaxLength = 2000;

    private PageSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid MerchantId { get; private set; }

    public Uri Address { get; private set; }

    public string Content { get; private set; }

    public DateTime FetchedOnUtc { get; private set; }

    public static Result<PageSnapshot> Capture(Merchant merchant, Uri address, string content, DateTime fetchedOnUtc)
    {
        if (!merchant.IsActive)
        {
            return Result.Failure<PageSnapshot>(MerchantErrors.Inactive(merchant.Id));
        }

        if (!merchant.Owns(address) || address.AbsoluteUri.Length > AddressMaxLength)
        {
            return Result.Failure<PageSnapshot>(MerchantErrors.AddressOutsideMerchant(address));
        }

        return new PageSnapshot
        {
            Id = Guid.CreateVersion7(),
            MerchantId = merchant.Id,
            Address = address,
            Content = content.Length > ContentMaxLength ? content[..ContentMaxLength] : content,
            FetchedOnUtc = fetchedOnUtc
        };
    }

    /// <summary>
    /// Whether the page shows this amount, written the French or the English way, with or without thousands separators
    /// (« 1 234,50 », « 1,234.50 », « 9,4 », « 9.40 »).
    /// </summary>
    public bool ShowsPrice(decimal amount)
    {
        decimal expected = decimal.Round(amount, 2);

        return Amounts().Matches(Content).Any(match => Parse(match.Value) == expected);
    }

    private static decimal? Parse(string written)
    {
        // The last separator followed by one or two digits is the decimal one; every other separator groups thousands.
        Match decimals = DecimalPart().Match(written);
        string integerPart = decimals.Success ? written[..decimals.Index] : written;
        string digits = string.Concat(integerPart.Where(char.IsAsciiDigit));
        string fraction = decimals.Success ? decimals.Groups[1].Value : "0";

        return decimal.TryParse($"{digits}.{fraction}", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value)
            ? decimal.Round(value, 2)
            : null;
    }

    [GeneratedRegex(@"\d{1,3}(?:[   .,']\d{3})+(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?", RegexOptions.CultureInvariant)]
    private static partial Regex Amounts();

    [GeneratedRegex(@"[.,](\d{1,2})$", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalPart();
}
