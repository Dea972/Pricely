namespace Pricely.Modules.Pricing.Domain.Offers;

/// <summary>
/// The identifiers of a merchant page compared with the reference product (<see cref="OfferMatchingPolicy.Compare"/>):
/// true identical, false different, null not shown by the page.
/// </summary>
public sealed record MatchComparison(bool? SameGtin, bool? SameBrand, bool? SameManufacturerReference);
