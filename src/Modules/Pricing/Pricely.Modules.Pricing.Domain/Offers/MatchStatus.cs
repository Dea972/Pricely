namespace Pricely.Modules.Pricing.Domain.Offers;

public enum MatchStatus
{
    /// <summary>Waiting for a human review before entering the comparison.</summary>
    Proposed = 0,

    /// <summary>Part of the price comparison.</summary>
    Confirmed = 1,

    /// <summary>Not the same product: excluded from the comparison and from future collections.</summary>
    Rejected = 2
}
