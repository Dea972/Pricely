using System.Text;

namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// Decides whether a merchant offer can be confirmed without human review.
/// Only strong identifiers confirm automatically: a single wrong match (a pack of 10 compared
/// with a unit) is enough to discredit the comparison. Everything else waits for a review.
/// </summary>
public static class OfferMatchingPolicy
{
    private const int GtinLength = 14;

    public static OfferConfirmationMethod? Evaluate(ProductIdentity product, MatchEvidence evidence)
    {
        if (HaveSameGtin(product.Gtin, evidence.ObservedGtin))
        {
            return OfferConfirmationMethod.Gtin;
        }

        if (HaveSameManufacturerReference(product, evidence))
        {
            return OfferConfirmationMethod.ManufacturerReference;
        }

        return null;
    }

    /// <summary>
    /// Identifier by identifier, what the reviewer of a proposed offer compares (same normalization as
    /// <see cref="Evaluate"/>): null when the merchant page does not show it.
    /// </summary>
    public static MatchComparison Compare(ProductIdentity product, MatchEvidence evidence) =>
        new(
            NormalizeGtin(evidence.ObservedGtin) is null ? null : HaveSameGtin(product.Gtin, evidence.ObservedGtin),
            NormalizeCode(evidence.ObservedBrand).Length == 0
                ? null
                : string.Equals(NormalizeCode(product.Brand), NormalizeCode(evidence.ObservedBrand), StringComparison.Ordinal),
            NormalizeCode(evidence.ObservedManufacturerReference).Length == 0
                ? null
                : string.Equals(NormalizeCode(product.ManufacturerReference), NormalizeCode(evidence.ObservedManufacturerReference), StringComparison.Ordinal));

    private static bool HaveSameGtin(string? productGtin, string? observedGtin)
    {
        string? left = NormalizeGtin(productGtin);
        string? right = NormalizeGtin(observedGtin);

        return left is not null && string.Equals(left, right, StringComparison.Ordinal);
    }

    private static bool HaveSameManufacturerReference(ProductIdentity product, MatchEvidence evidence)
    {
        string productReference = NormalizeCode(product.ManufacturerReference);
        string productBrand = NormalizeCode(product.Brand);

        return productReference.Length > 0 &&
               productBrand.Length > 0 &&
               string.Equals(productReference, NormalizeCode(evidence.ObservedManufacturerReference), StringComparison.Ordinal) &&
               string.Equals(productBrand, NormalizeCode(evidence.ObservedBrand), StringComparison.Ordinal);
    }

    /// <summary>
    /// GTIN-8, GTIN-12, GTIN-13 and GTIN-14 are compared on 14 digits (left padded with zeros).
    /// </summary>
    private static string? NormalizeGtin(string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
        {
            return null;
        }

        string digits = string.Concat(gtin.Where(char.IsAsciiDigit));

        return digits.Length is 8 or 12 or 13 or 14 ? digits.PadLeft(GtinLength, '0') : null;
    }

    /// <summary>
    /// Keeps letters and digits only, upper-cased: "CEM-II 32,5" and "cem ii 325" are equal.
    /// </summary>
    internal static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        foreach (char character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }
}
