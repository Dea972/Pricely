using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Offers;

public static class OfferErrors
{
    public static readonly Error InvalidPrice = Error.Problem(
        "Offers.InvalidPrice",
        "A price must be strictly positive");

    public static Error InvalidCurrency(string currency) =>
        Error.Problem("Offers.InvalidCurrency", $"'{currency}' is not an ISO 4217 currency code");

    public static Error MerchantNotInCollectionPlan(Guid merchantId, Guid productId) =>
        Error.Problem(
            "Offers.MerchantNotInCollectionPlan",
            $"The merchant {merchantId} is not in the collection plan of the product {productId}");

    public static Error NotFound(Guid offerId) =>
        Error.NotFound("Offers.NotFound", $"The offer with the identifier {offerId} was not found");

    public static Error NotPendingReview(Guid offerId, MatchStatus status) =>
        Error.Conflict("Offers.NotPendingReview", $"The offer {offerId} is {status} and can no longer be reviewed");

    public static Error Rejected(Guid offerId) =>
        Error.Conflict("Offers.Rejected", $"The offer {offerId} was rejected and does not accept price observations");
}
