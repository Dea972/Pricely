using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Abstractions.Collection;

/// <summary>
/// A merchant page found by a collector, normalized.
/// </summary>
public sealed class CollectedOffer
{
    public required Guid MerchantId { get; init; }

    public required Uri ProductPage { get; init; }

    public string? MerchantProductReference { get; init; }

    public required decimal PriceAmount { get; init; }

    public required string Currency { get; init; }

    public Availability Availability { get; init; }

    public required DateTime ObservedOnUtc { get; init; }

    public required MatchEvidence Evidence { get; init; }
}
