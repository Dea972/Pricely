using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Pricely.Common.Infrastructure.Web;

/// <summary>
/// The readable text of a web page for the agents: visible text, plus the structured product data (JSON-LD, price and
/// product meta tags, microdata) where sites often put the price, the GTIN and the brand. Also the title of the page
/// and the search pages its markup declares.
/// </summary>
internal static partial class HtmlText
{
    public const string QueryPlaceholder = "{query}";

    private const int TimeoutMilliseconds = 2000;

    private const int MaxSearchTemplates = 5;

    /// <summary>Names a search field often has (<c>q</c>, <c>search</c>, <c>k</c>, <c>recherche</c>...).</summary>
    private static readonly HashSet<string> SearchFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "q", "query", "search", "s", "k", "text", "term", "keyword", "keywords", "searchterm", "search_query", "recherche",
        "motcle", "w", "ntt", "kw", "searchtext"
    };

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

    /// <summary><c>og:site_name</c>, else <c>&lt;title&gt;</c>.</summary>
    public static string? Title(string html)
    {
        Match siteName = SiteName().Match(html);
        Match title = siteName.Success ? siteName : TitleTag().Match(html);

        return title.Success ? Collapse(WebUtility.HtmlDecode(title.Groups["value"].Value)) : null;
    }

    /// <summary>
    /// The search pages the markup declares, absolute, <see cref="QueryPlaceholder"/> standing for the searched text:
    /// <c>SearchAction</c> of the structured data first (the site states it), then the GET forms with a search field.
    /// </summary>
    public static IReadOnlyList<string> SearchTemplates(string html, Uri address)
    {
        var templates = new List<string>();

        foreach (Match action in SearchActionTarget().Matches(html))
        {
            string target = action.Groups["target"].Value.Replace(@"\/", "/", StringComparison.Ordinal);
            Add(templates, PlaceholderVariable().Replace(target, QueryPlaceholder), address);
        }

        foreach (Match form in Forms().Matches(html))
        {
            string attributes = form.Groups["attributes"].Value;
            string method = Attribute(attributes, "method") ?? "get";
            if (!string.Equals(method, "get", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (FormTemplate(Attribute(attributes, "action"), form.Groups["body"].Value) is string template)
            {
                Add(templates, template, address);
            }
        }

        return templates;
    }

    private static string? FormTemplate(string? action, string body)
    {
        var parameters = new List<string>();
        string? searchField = null;

        foreach (Match input in Inputs().Matches(body))
        {
            string attributes = input.Groups["attributes"].Value;
            string? name = Attribute(attributes, "name");
            string type = Attribute(attributes, "type") ?? "text";
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (string.Equals(type, "hidden", StringComparison.OrdinalIgnoreCase))
            {
                parameters.Add(Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(WebUtility.HtmlDecode(Attribute(attributes, "value") ?? string.Empty)));
            }
            else if (searchField is null
                && (string.Equals(type, "search", StringComparison.OrdinalIgnoreCase) || SearchFieldNames.Contains(name))
                && type is not ("submit" or "checkbox" or "radio" or "button"))
            {
                searchField = name;
            }
        }

        if (searchField is null)
        {
            return null;
        }

        parameters.Add(Uri.EscapeDataString(searchField) + "=" + QueryPlaceholder);
        string path = WebUtility.HtmlDecode(action ?? string.Empty).Split('?')[0];

        return path + "?" + string.Join('&', parameters);
    }

    private static void Add(List<string> templates, string template, Uri address)
    {
        if (templates.Count >= MaxSearchTemplates || !template.Contains(QueryPlaceholder, StringComparison.Ordinal))
        {
            return;
        }

        // The query part is kept as written: Uri would escape the braces of the placeholder.
        int queryStart = template.IndexOf('?', StringComparison.Ordinal);
        string path = queryStart < 0 ? template : template[..queryStart];
        string query = queryStart < 0 ? string.Empty : template[queryStart..];

        if (!Uri.TryCreate(address, path, out Uri? resolved) || resolved.Scheme is not ("http" or "https"))
        {
            return;
        }

        string absolute = resolved.GetLeftPart(UriPartial.Path).Replace("%7Bquery%7D", QueryPlaceholder, StringComparison.OrdinalIgnoreCase) + query;

        if (!templates.Contains(absolute, StringComparer.Ordinal))
        {
            templates.Add(absolute);
        }
    }

    private static string? Attribute(string attributes, string name)
    {
        Match match = AttributeValue().Matches(attributes).FirstOrDefault(m => string.Equals(m.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase));

        return match?.Groups["value"].Value;
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

    [GeneratedRegex(@"<meta[^>]*property\s*=\s*[""']og:site_name[""'][^>]*content\s*=\s*[""'](?<value>[^""']*)[""']", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex SiteName();

    [GeneratedRegex(@"<title[^>]*>(?<value>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeoutMilliseconds)]
    private static partial Regex TitleTag();

    // "potentialAction": {"@type": "SearchAction", "target": "https://site/search?q={search_term_string}", ...}
    [GeneratedRegex(@"""target""\s*:\s*(?:\{[^{}]*?""urlTemplate""\s*:\s*)?""(?<target>[^""]*\{[a-z_]+\}[^""]*)""", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex SearchActionTarget();

    [GeneratedRegex(@"\{[a-z_]+\}", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex PlaceholderVariable();

    [GeneratedRegex(@"<form\b(?<attributes>[^>]*)>(?<body>.*?)</form\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeoutMilliseconds)]
    private static partial Regex Forms();

    [GeneratedRegex(@"<input\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex Inputs();

    [GeneratedRegex(@"(?<name>[a-z-]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s>]+))", RegexOptions.IgnoreCase, TimeoutMilliseconds)]
    private static partial Regex AttributeValue();
}
