using System.Text.Json.Serialization;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;

/// <summary>
/// An offer read by a collector (agent or MCP client) on a page that Pricely fetched (<see cref="SnapshotId"/>).
/// </summary>
public sealed record SubmittedOffer
{
    /// <summary>The page the offer was read on (<c>fetch_merchant_page</c>): the price must appear in it.</summary>
    public required Guid SnapshotId { get; init; }

    public required decimal Price { get; init; }

    public string Currency { get; init; } = "EUR";

    [JsonConverter(typeof(JsonStringEnumConverter<Availability>))]
    public Availability Availability { get; init; }

    /// <summary>Reference of the product at the merchant (SKU), if displayed.</summary>
    public string? MerchantProductReference { get; init; }

    public string? ObservedTitle { get; init; }

    public string? ObservedGtin { get; init; }

    public string? ObservedBrand { get; init; }

    public string? ObservedManufacturerReference { get; init; }

    /// <summary>How sure the collector is that it is the same product, between 0 and 1.</summary>
    public decimal? Similarity { get; init; }

    /// <summary>Why the collector thinks it is the same product (or not), for the reviewer.</summary>
    public string? Rationale { get; init; }
}
