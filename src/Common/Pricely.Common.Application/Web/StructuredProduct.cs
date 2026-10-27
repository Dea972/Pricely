namespace Pricely.Common.Application.Web;

/// <summary>
/// A product that a page declares in its structured data (schema.org <c>Product</c> in JSON-LD): what the site states
/// about the product it shows, more reliable than the visible text (no shipping cost, no eco-contribution, no other
/// product of a carousel).
/// </summary>
/// <param name="Name">Name of the product.</param>
/// <param name="Gtin">GTIN (<c>gtin13</c>, <c>gtin14</c>, <c>gtin12</c>, <c>gtin8</c> or <c>gtin</c>), as written.</param>
/// <param name="Brand">Brand name.</param>
/// <param name="Sku">Reference of the product at the merchant.</param>
/// <param name="Offers">The prices the page declares for it, in the order of the page (the main offer first).</param>
public sealed record StructuredProduct(
    string? Name,
    string? Gtin,
    string? Brand,
    string? Sku,
    IReadOnlyList<StructuredOffer> Offers);

/// <summary>A price a page declares for a product (schema.org <c>Offer</c>, or a bound of an <c>AggregateOffer</c>).</summary>
/// <param name="Price">The amount, always positive (a missing or zero price is not an offer).</param>
/// <param name="Currency">ISO 4217 code, if declared.</param>
/// <param name="Availability">schema.org availability without its prefix (<c>InStock</c>, <c>OutOfStock</c>...), if declared.</param>
/// <param name="Seller">Name of the seller, when the page declares one (a marketplace seller, for example).</param>
/// <param name="IsAggregate">Whether the price is a bound of an <c>AggregateOffer</c> (lowest or highest of several sellers).</param>
public sealed record StructuredOffer(
    decimal Price,
    string? Currency,
    string? Availability,
    string? Seller,
    bool IsAggregate = false);
