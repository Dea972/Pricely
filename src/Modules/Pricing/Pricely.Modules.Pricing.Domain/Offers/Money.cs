using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// A strictly positive amount in an ISO 4217 currency (EUR, USD...).
/// </summary>
public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            return Result.Failure<Money>(OfferErrors.InvalidPrice);
        }

        string normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3 || !normalizedCurrency.All(char.IsAsciiLetterUpper))
        {
            return Result.Failure<Money>(OfferErrors.InvalidCurrency(currency));
        }

        return new Money(amount, normalizedCurrency);
    }
}
