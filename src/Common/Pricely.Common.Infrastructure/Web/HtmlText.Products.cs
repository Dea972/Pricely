using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pricely.Common.Application.Web;

namespace Pricely.Common.Infrastructure.Web;

/// <summary>
/// The products a page declares in JSON-LD (schema.org <c>Product</c> with its <c>offers</c>). A malformed block is
/// skipped: sites often ship broken JSON-LD, and the visible text stays the fallback.
/// </summary>
internal static partial class HtmlText
{
    private const int MaxProducts = 10;

    private const int MaxOffersPerProduct = 20;

    private const int MaxDepth = 8;

    private static readonly JsonDocumentOptions JsonOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    private static readonly string[] GtinProperties = ["gtin13", "gtin14", "gtin12", "gtin8", "gtin"];

    public static IReadOnlyList<StructuredProduct> Products(string html)
    {
        var products = new List<StructuredProduct>();

        foreach (Match block in JsonLd().Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(WebUtility.HtmlDecode(block.Groups[1].Value), JsonOptions);
                Collect(document.RootElement, products, depth: 0);
            }
            catch (JsonException)
            {
                // Broken JSON-LD: the visible text remains.
            }

            if (products.Count >= MaxProducts)
            {
                break;
            }
        }

        return products;
    }

    private static void Collect(JsonElement element, List<StructuredProduct> products, int depth)
    {
        if (depth > MaxDepth || products.Count >= MaxProducts)
        {
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                Collect(item, products, depth + 1);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (IsType(element, "Product"))
        {
            products.Add(Product(element));

            return;
        }

        // A WebPage or an ItemPage often holds its product in @graph or mainEntity.
        foreach (string container in (string[])["@graph", "mainEntity", "itemListElement", "item"])
        {
            if (element.TryGetProperty(container, out JsonElement inner))
            {
                Collect(inner, products, depth + 1);
            }
        }
    }

    private static StructuredProduct Product(JsonElement product)
    {
        string? gtin = GtinProperties.Select(name => Text(product, name)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        string? brand = Brand(product);

        var offers = new List<StructuredOffer>();
        if (product.TryGetProperty("offers", out JsonElement offersElement))
        {
            foreach (JsonElement offer in offersElement.ValueKind == JsonValueKind.Array ? offersElement.EnumerateArray() : Enumerable.Repeat(offersElement, 1))
            {
                AddOffers(offer, offers);
                if (offers.Count >= MaxOffersPerProduct)
                {
                    break;
                }
            }
        }

        return new StructuredProduct(Text(product, "name"), gtin, brand, Text(product, "sku"), offers);
    }

    private static void AddOffers(JsonElement offer, List<StructuredOffer> offers)
    {
        if (offer.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        string? currency = Text(offer, "priceCurrency");
        string? availability = Availability(Text(offer, "availability"));

        if (IsType(offer, "AggregateOffer"))
        {
            // An aggregate of no offer (« offerCount: 0, lowPrice: 0 ») is an unavailable product, not a price.
            if (offer.TryGetProperty("offerCount", out JsonElement count) && Amount(count) is <= 0)
            {
                return;
            }

            foreach (string bound in (string[])["lowPrice", "highPrice"])
            {
                if (offer.TryGetProperty(bound, out JsonElement value) && Amount(value) is decimal price && offers.TrueForAll(o => o.Price != price))
                {
                    offers.Add(new StructuredOffer(price, currency, availability, Seller: null, IsAggregate: true));
                }
            }

            return;
        }

        if (offer.TryGetProperty("price", out JsonElement amount) && Amount(amount) is decimal offerPrice)
        {
            string? seller = offer.TryGetProperty("seller", out JsonElement sellerElement) && sellerElement.ValueKind == JsonValueKind.Object
                ? Text(sellerElement, "name")
                : null;
            offers.Add(new StructuredOffer(offerPrice, currency, availability, seller));
        }
    }

    /// <summary>« "brand": "Lafarge" » or « "brand": {"@type": "Brand", "name": "Lafarge"} ».</summary>
    private static string? Brand(JsonElement product)
    {
        if (!product.TryGetProperty("brand", out JsonElement brand))
        {
            return null;
        }

        return brand.ValueKind == JsonValueKind.Object ? Text(brand, "name") : Scalar(brand);
    }

    /// <summary>A positive amount, written as a number or a text (« 17.64 », « 17,64 »); null otherwise.</summary>
    private static decimal? Amount(JsonElement value)
    {
        decimal? amount = value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out decimal number) => number,
            JsonValueKind.String when decimal.TryParse(
                value.GetString()?.Trim().Replace(',', '.'),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal parsed) => parsed,
            _ => null
        };

        return amount > 0 ? decimal.Round(amount.Value, 2) : null;
    }

    /// <summary>« https://schema.org/InStock » or « InStock » → « InStock ».</summary>
    private static string? Availability(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/').Split('/')[^1];

    private static bool IsType(JsonElement element, string type) =>
        element.TryGetProperty("@type", out JsonElement value)
        && (value.ValueKind == JsonValueKind.String
            ? string.Equals(value.GetString(), type, StringComparison.OrdinalIgnoreCase)
            : value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(t => string.Equals(t.GetString(), type, StringComparison.OrdinalIgnoreCase)));

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) ? Scalar(value) : null;

    private static string? Scalar(JsonElement value)
    {
        string? text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(text) ? null : WebUtility.HtmlDecode(text).Trim();
    }
}
