using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.PageSnapshots;

public static class PageSnapshotErrors
{
    public static Error NotFound(Guid snapshotId) =>
        Error.NotFound("PageSnapshots.NotFound", $"The page snapshot {snapshotId} was not found");

    public static Error PriceNotShown(decimal amount) =>
        Error.Problem(
            "PageSnapshots.PriceNotShown",
            $"The price {amount.ToString(System.Globalization.CultureInfo.InvariantCulture)} does not appear in the fetched page");

    public static Error NoAddress(Guid merchantId) =>
        Error.Problem(
            "PageSnapshots.NoAddress",
            $"Give the address of a page: the merchant {merchantId} has no search address template");

    public static Error TooManyRedirects(Uri address) =>
        Error.Problem("PageSnapshots.TooManyRedirects", $"The page {address} redirects too many times");

    public static Error Unreadable(Uri address, string reason) =>
        Error.Problem("PageSnapshots.Unreadable", $"The page {address} could not be read: {reason}");

    public static Error OtherMerchant(Guid snapshotId) =>
        Error.Problem("PageSnapshots.OtherMerchant", $"The page snapshot {snapshotId} belongs to another merchant");
}
