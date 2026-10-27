using Pricely.Common.Domain;

namespace Pricely.Modules.Catalog.Domain.Products;

/// <summary>
/// Global Trade Item Number (EAN-8, UPC-A, EAN-13 or GTIN-14), validated with its check digit.
/// </summary>
public sealed record Gtin
{
    private static readonly int[] Lengths = [8, 12, 13, 14];

    private Gtin(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Gtin> Create(string value)
    {
        string digits = value.Trim();

        if (digits.Length is not (8 or 12 or 13 or 14) || !digits.All(char.IsAsciiDigit))
        {
            return Result.Failure<Gtin>(ProductErrors.InvalidGtin(value));
        }

        if (!HasValidCheckDigit(digits))
        {
            return Result.Failure<Gtin>(ProductErrors.InvalidGtin(value));
        }

        return new Gtin(digits);
    }

    /// <summary>
    /// The ways the same GTIN can be written (8, 12, 13 or 14 digits, padded with leading zeros), this one included: a
    /// product registered with an EAN-13 is the product of the same GTIN-14.
    /// </summary>
    public IReadOnlyList<string> EquivalentForms()
    {
        string full = Value.PadLeft(14, '0');

        return [.. Lengths
            .Where(length => full[..(14 - length)].All(c => c == '0'))
            .Select(length => full[(14 - length)..])];
    }

    private static bool HasValidCheckDigit(string digits)
    {
        int sum = 0;
        int lastIndex = digits.Length - 1;

        for (int index = lastIndex - 1; index >= 0; index--)
        {
            int digit = digits[index] - '0';
            int positionFromRight = lastIndex - index;
            sum += positionFromRight % 2 == 1 ? digit * 3 : digit;
        }

        int expectedCheckDigit = (10 - sum % 10) % 10;

        return digits[lastIndex] - '0' == expectedCheckDigit;
    }
}
