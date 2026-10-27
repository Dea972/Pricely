using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Offers.AddWebOffer;

/// <summary>
/// Records an offer found by web research (ADR-028): the page of a followed merchant is read again when the command runs
/// (the user approves later than the model read it), and the price must be on that fresh page, as for a collection. The
/// product is identified by its GTIN, or its brand and manufacturer reference, so that it can be added in the same batch.
/// If the merchant is not among the merchants of the product's typology, it is added to its collection profile.
/// </summary>
/// <param name="PageAddress">The product page, on the site of a followed merchant.</param>
/// <param name="Price">The price the model read on the page, checked against the fresh page.</param>
public sealed record AddWebOfferCommand(
    string? ProductGtin,
    string? ProductBrand,
    string? ProductReference,
    Uri PageAddress,
    decimal Price,
    string Currency) : ICommand<AddedWebOfferResponse>;

/// <param name="Outcome">What happened to the offer (<c>NewOfferProposed</c>, <c>NewOfferConfirmed</c>, <c>ExistingOfferObserved</c>...).</param>
/// <param name="AddedToProfile">Whether the merchant was added to the collection profile of the product's typology.</param>
public sealed record AddedWebOfferResponse(
    Guid ProductId,
    string ProductName,
    Guid MerchantId,
    string MerchantName,
    Uri PageAddress,
    decimal Price,
    string Currency,
    string Outcome,
    bool IsFlaggedAsAnomaly,
    bool AddedToProfile);
