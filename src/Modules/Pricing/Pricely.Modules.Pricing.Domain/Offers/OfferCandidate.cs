namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// A merchant page found by the collector for a reference product.
/// </summary>
public sealed record OfferCandidate(
    Guid ProductId,
    Guid MerchantId,
    Uri ProductPage,
    string? MerchantProductReference,
    MatchEvidence Evidence);
