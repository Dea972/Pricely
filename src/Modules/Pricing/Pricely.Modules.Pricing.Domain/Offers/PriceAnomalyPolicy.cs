namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// Flags a price that deviates strongly from the median of the other confirmed offers of the same product.
/// It is the typical symptom of a wrong match or a different packaging, even on a confirmed offer.
/// </summary>
public static class PriceAnomalyPolicy
{
    public const decimal MaximumDeviationFromMedian = 0.40m;

    /// <param name="observedPrice">The price just observed.</param>
    /// <param name="referencePrices">Latest prices of the other confirmed offers of the product.</param>
    public static bool IsAnomalous(Money observedPrice, IReadOnlyCollection<Money> referencePrices)
    {
        decimal[] sameCurrencyAmounts = referencePrices
            .Where(price => string.Equals(price.Currency, observedPrice.Currency, StringComparison.Ordinal))
            .Select(price => price.Amount)
            .Order()
            .ToArray();

        if (sameCurrencyAmounts.Length == 0)
        {
            return false;
        }

        decimal median = Median(sameCurrencyAmounts);

        return Math.Abs(observedPrice.Amount - median) / median > MaximumDeviationFromMedian;
    }

    private static decimal Median(decimal[] sortedAmounts)
    {
        int middle = sortedAmounts.Length / 2;

        return sortedAmounts.Length % 2 == 1
            ? sortedAmounts[middle]
            : (sortedAmounts[middle - 1] + sortedAmounts[middle]) / 2;
    }
}
