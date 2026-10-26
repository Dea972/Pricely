using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

/// <summary>
/// The readable text of a merchant page for the collection agent: visible text, plus the structured product data
/// (JSON-LD, price and product meta tags, microdata) where sites often put the price, the GTIN and the brand.
/// </summary>
internal static partial class HtmlText
{
    private const int TimeoutMilliseconds = 2000;

    public static string Extract(string html)
    {
        var text = new StringBuilder();

        foreach (Match structured in JsonLd().Matches(html))
        {
            text.AppendLine("[données structurées] " + Collapse(structured.Groups[1].Value));
        }

        foreach (Match meta in ProductAttributes().Matches(html))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"[{meta.Groups["name"].Value}] {WebUtility.HtmlDecode(meta.Groups["value"].Value)}");
        }

        string visible = Hidden().Replace(html, " ");
        visible = Comments().Replace(visible, " ");
        visible = BlockEnd().Replace(visible, "\n");
        visible = Tags().Replace(visible, " ");
        visible = WebUtility.HtmlDecode(visible);

        foreach (string line in visible.Split('\n'))
        {
            string collapsed = Collapse(line);
            if (collapsed.Length > 0)
            {
                text.AppendLine(collapsed);
            }
        }

        return text.ToString();
    }

    private static string Collapse(string value) => Spaces().Replace(value, " ").Trim();

    [GeneratedRegex(@"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeoutMilliseconds)]
    private static partial Regex JsonLd();

    // <meta property="product:price:amount" content="9.40">, <span itemprop="price" content="9.40">...
    [GeneratedRegex(@"<[a-z]+[^>]*(?:property|name|itemprop)\s*=\s*[""'](?<name>[^""']*(?:price|currency|availability|gtin|ean|sku|brand|og:title|product)[^""']*)[""'][^>]*content\s*=\s*[""'](?<value>[^""']*)[""']", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex ProductAttributes();

    [GeneratedRegex(@"<(script|style|noscript|svg|template|iframe)\b.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeoutMilliseconds)]
    private static partial Regex Hidden();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline, TimeoutMilliseconds)]
    private static partial Regex Comments();

    [GeneratedRegex(@"<(br|/p|/div|/li|/tr|/h[1-6]|/section|/article|/td|/th|/dt|/dd|/title)\b[^>]*>", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex BlockEnd();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.None, TimeoutMilliseconds)]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+", RegexOptions.None, TimeoutMilliseconds)]
    private static partial Regex Spaces();
}
